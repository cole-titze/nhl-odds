using DatabaseAccess;
using DatabaseAccess.BookmakerOddsRepository;
using Entities.DbModels;
using Entities.ServiceModels.Kalshi;
using Entities.ServiceModels.Mappers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Services.Kalshi;

namespace BookmakerOddsGetter;

public class KalshiOddsBackfiller
{
    private const int BACKFILL_START_YEAR = 2025;
    private const string BOOKMAKER_NAME = "Kalshi";
    private const string BOOKMAKER_KEY = "kalshi";
    private const double STANDARD_PUCK_LINE = 1.5;
    private const double DEFAULT_TOTAL_LINE = 5.5;

    // Sentinel dates for caching in BookmakerOddsResponse.QueryDateUTC
    private static readonly Dictionary<string, DateTime> CacheSentinels = new()
    {
        ["settled-KXNHLGAME"] = new DateTime(1900, 1, 1),
        ["settled-KXNHLSPREAD"] = new DateTime(1900, 1, 2),
        ["settled-KXNHLTOTAL"] = new DateTime(1900, 1, 3),
        ["candles-KXNHLGAME"] = new DateTime(1900, 2, 1),
        ["candles-KXNHLSPREAD"] = new DateTime(1900, 2, 2),
        ["candles-KXNHLTOTAL"] = new DateTime(1900, 2, 3),
    };

    private static readonly TimeZoneInfo CentralZone = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");

    private readonly NhlDbContext _dbContext;
    private readonly IBookmakerOddsRepository _bookmakerOddsRepo;
    private readonly IKalshiGetter _kalshiGetter;
    private readonly ILogger<KalshiOddsBackfiller> _logger;

    public KalshiOddsBackfiller(
        NhlDbContext dbContext,
        IBookmakerOddsRepository bookmakerOddsRepo,
        IKalshiGetter kalshiGetter,
        ILoggerFactory loggerFactory)
    {
        _dbContext = dbContext;
        _bookmakerOddsRepo = bookmakerOddsRepo;
        _kalshiGetter = kalshiGetter;
        _logger = loggerFactory.CreateLogger<KalshiOddsBackfiller>();
    }

    public async Task BackfillKalshiOdds()
    {
        // 1. Find all played games — always process everything so cached data
        // gets re-matched when matching logic is updated. AddOrUpdate handles dedup.
        var playedGames = await _dbContext.GameRaw
            .Where(g => g.HasBeenPlayed && g.SeasonStartYear >= BACKFILL_START_YEAR)
            .Select(g => new { g.Id, g.HomeTeamId, g.AwayTeamId, g.SeasonStartYear, g.GameDateUTC })
            .ToListAsync();

        if (!playedGames.Any())
        {
            _logger.LogInformation("No played games found for backfill.");
            return;
        }

        _logger.LogInformation("Found {Total} played games for Kalshi backfill", playedGames.Count);
        var gamesToBackfill = playedGames;

        // 2. Load season teams for name matching
        var seasonYears = gamesToBackfill.Select(g => g.SeasonStartYear).Distinct().ToList();
        var allSeasonTeams = await _dbContext.SeasonTeam
            .Where(t => seasonYears.Contains(t.SeasonStartYear))
            .ToListAsync();
        var seasonTeamsByYear = allSeasonTeams
            .GroupBy(t => t.SeasonStartYear)
            .ToDictionary(g => g.Key, g => g.ToDictionary(t => t.TeamId, t => t.Name));

        // 3. Fetch settled market lists (cached for 7 days since they're large but don't change for old games)
        _logger.LogInformation("Fetching settled market lists...");
        var allGameMarkets = await GetOrFetchSettledMarkets("settled-KXNHLGAME", "KXNHLGAME");
        var allSpreadMarkets = await GetOrFetchSettledMarkets("settled-KXNHLSPREAD", "KXNHLSPREAD");
        var allTotalMarkets = await GetOrFetchSettledMarkets("settled-KXNHLTOTAL", "KXNHLTOTAL");

        // Check if any recent games need backfill — if so, the cache might be stale
        var hasRecentGames = gamesToBackfill.Any(g => g.GameDateUTC >= DateTime.UtcNow.AddDays(-3));
        if (hasRecentGames)
        {
            // Re-fetch fresh data for recent games
            _logger.LogInformation("Recent games need backfill — fetching fresh settled markets...");
            var freshGame = await _kalshiGetter.GetSettledMarkets("KXNHLGAME");
            var freshSpread = await _kalshiGetter.GetSettledMarkets("KXNHLSPREAD");
            var freshTotal = await _kalshiGetter.GetSettledMarkets("KXNHLTOTAL");

            // Merge: fresh data wins for duplicate tickers
            var existingGameTickers = allGameMarkets.Select(m => m.Ticker).ToHashSet();
            allGameMarkets.AddRange(freshGame.Where(m => !existingGameTickers.Contains(m.Ticker)));
            var existingSpreadTickers = allSpreadMarkets.Select(m => m.Ticker).ToHashSet();
            allSpreadMarkets.AddRange(freshSpread.Where(m => !existingSpreadTickers.Contains(m.Ticker)));
            var existingTotalTickers = allTotalMarkets.Select(m => m.Ticker).ToHashSet();
            allTotalMarkets.AddRange(freshTotal.Where(m => !existingTotalTickers.Contains(m.Ticker)));
        }

        _logger.LogInformation("Total markets: {Game} game, {Spread} spread, {Total} total",
            allGameMarkets.Count, allSpreadMarkets.Count, allTotalMarkets.Count);

        // 4. Match markets to games and fetch candlestick prices
        var gameDatesAll = gamesToBackfill.ToDictionary(g => g.Id, g => g.GameDateUTC);
        var gameInfoAll = new List<OddsApiResponseMapper.GameInfo>();
        foreach (var seasonGroup in gamesToBackfill.GroupBy(g => g.SeasonStartYear))
        {
            if (!seasonTeamsByYear.TryGetValue(seasonGroup.Key, out var seasonTeams)) continue;
            gameInfoAll.AddRange(BookmakerOddsHelper.BuildGameInfoList(
                seasonGroup.Select(g => new GameRef(g.Id, g.HomeTeamId, g.AwayTeamId)),
                seasonTeams, gameDatesAll));
        }

        // Build event-ticker -> gameId mapping using the existing matching logic
        var h2hEventToGame = MatchEventsToGames(allGameMarkets, gameInfoAll, MarketType.H2H);
        var spreadEventToGame = MatchEventsToGames(allSpreadMarkets.Where(m => m.FloorStrike is STANDARD_PUCK_LINE).ToList(), gameInfoAll, MarketType.Spread);
        var totalEventToGame = MatchEventsToGames(allTotalMarkets, gameInfoAll, MarketType.Total);

        _logger.LogInformation("Matched events: {H2H} h2h, {Spread} spread, {Total} total",
            h2hEventToGame.Count, spreadEventToGame.Count, totalEventToGame.Count);

        // 5. Load cached candlestick data from DB
        await LoadCandleCacheFromDb();

        // 6. Fetch candlestick prices for each matched market and build odds records
        var h2hRecords = new List<DbBookmakerOdds>();
        var spreadRecords = new List<DbBookmakerSpreads>();
        var totalRecords = new List<DbBookmakerTotals>();

        var processed = 0;
        var totalEvents = h2hEventToGame.Count + spreadEventToGame.Count + totalEventToGame.Count;

        // H2H: fetch candlesticks for each event's market pair
        foreach (var (eventTicker, match) in h2hEventToGame)
        {
            if (match.Markets.Count != 2) continue;

            var gameDate = gameDatesAll[match.GameId];
            var game = gameInfoAll.First(g => g.GameId == match.GameId);
            var (homeMarket, awayMarket) = KalshiResponseMapper.MatchHomeAway(match.Markets, game);
            if (homeMarket == null || awayMarket == null) continue;

            var homeCandle = await FetchGameDayCandle("KXNHLGAME", homeMarket.Ticker, gameDate);
            var awayCandle = await FetchGameDayCandle("KXNHLGAME", awayMarket.Ticker, gameDate);
            processed++;

            if (homeCandle == null || awayCandle == null) continue;
            // Each h2h market is "Team wins?" — use yes_ask as the implied probability (includes vig)
            var homeOdds = homeCandle.YesAsk.Close;
            var awayOdds = awayCandle.YesAsk.Close;
            if (homeOdds <= 0 || awayOdds <= 0) continue;

            h2hRecords.Add(new DbBookmakerOdds
            {
                GameId = match.GameId,
                BookmakerName = BOOKMAKER_NAME,
                BookmakerKey = BOOKMAKER_KEY,
                HomeOdds = homeOdds,
                AwayOdds = awayOdds,
                FetchedDateUTC = Get6amCentralUtc(gameDate),
                BookmakerLastUpdate = Get6amCentralUtc(gameDate),
                MarketLastUpdate = Get6amCentralUtc(gameDate),
                OddsApiGameId = eventTicker,
            });

            if (processed % 100 == 0)
                _logger.LogInformation("Progress: {Processed}/{Total} events", processed, totalEvents);
        }

        // Spreads
        foreach (var (eventTicker, match) in spreadEventToGame)
        {
            if (match.Markets.Count != 2) continue;

            var gameDate = gameDatesAll[match.GameId];
            var game = gameInfoAll.First(g => g.GameId == match.GameId);
            var spreadMatch = KalshiResponseMapper.MatchHomeAwayFromNames(match.Markets, game,
                m => KalshiResponseMapper.ExtractSpreadTeamName(m.YesSubTitle));
            if (spreadMatch.home == null || spreadMatch.away == null) continue;
            var homeMarket = spreadMatch.home;
            var awayMarket = spreadMatch.away;

            var homeCandle = await FetchGameDayCandle("KXNHLSPREAD", homeMarket.Ticker, gameDate);
            var awayCandle = await FetchGameDayCandle("KXNHLSPREAD", awayMarket.Ticker, gameDate);
            processed++;

            if (homeCandle == null) continue;
            var yesBid = homeCandle.YesBid.Close;
            var yesAsk = homeCandle.YesAsk.Close;
            if (yesAsk <= 0) continue;

            // Single contract: "Home wins by over 1.5"
            // Buy YES (Home -1.5) at yes_ask price
            // Buy NO (Away +1.5) at 1 - yes_bid price
            // The bid/ask spread is the vig: yes_ask + (1 - yes_bid) > 1
            spreadRecords.Add(new DbBookmakerSpreads
            {
                GameId = match.GameId,
                BookmakerName = BOOKMAKER_NAME,
                BookmakerKey = BOOKMAKER_KEY,
                HomePoint = -STANDARD_PUCK_LINE,
                HomePrice = OddsApiResponseMapper.ImpliedProbabilityToAmerican(yesAsk),
                AwayPoint = STANDARD_PUCK_LINE,
                AwayPrice = OddsApiResponseMapper.ImpliedProbabilityToAmerican(1.0 - yesBid),
                FetchedDateUTC = Get6amCentralUtc(gameDate),
                BookmakerLastUpdate = Get6amCentralUtc(gameDate),
                MarketLastUpdate = Get6amCentralUtc(gameDate),
                OddsApiGameId = eventTicker,
            });

            if (processed % 100 == 0)
                _logger.LogInformation("Progress: {Processed}/{Total} events", processed, totalEvents);
        }

        // Totals
        foreach (var (eventTicker, match) in totalEventToGame)
        {
            if (match.Markets.Count == 0) continue;

            var gameDate = gameDatesAll[match.GameId];
            var bestLine = match.Markets
                .Where(m => m.FloorStrike.HasValue)
                .OrderBy(m => Math.Abs(m.FloorStrike!.Value - DEFAULT_TOTAL_LINE))
                .FirstOrDefault();
            if (bestLine == null) continue;

            var candle = await FetchGameDayCandle("KXNHLTOTAL", bestLine.Ticker, gameDate);
            processed++;

            if (candle == null) continue;
            var yesAsk = candle.YesAsk.Close;
            var yesBid = candle.YesBid.Close;
            if (yesAsk <= 0) continue;

            // Single contract: "Over X.5 goals"
            // Buy YES (Over) at yes_ask, Buy NO (Under) at 1 - yes_bid
            totalRecords.Add(new DbBookmakerTotals
            {
                GameId = match.GameId,
                BookmakerName = BOOKMAKER_NAME,
                BookmakerKey = BOOKMAKER_KEY,
                OverUnderPoint = bestLine.FloorStrike!.Value,
                OverPrice = OddsApiResponseMapper.ImpliedProbabilityToAmerican(yesAsk),
                UnderPrice = OddsApiResponseMapper.ImpliedProbabilityToAmerican(1.0 - yesBid),
                FetchedDateUTC = Get6amCentralUtc(gameDate),
                BookmakerLastUpdate = Get6amCentralUtc(gameDate),
                MarketLastUpdate = Get6amCentralUtc(gameDate),
                OddsApiGameId = eventTicker,
            });

            if (processed % 100 == 0)
                _logger.LogInformation("Progress: {Processed}/{Total} events", processed, totalEvents);
        }

        // 7. Save candlestick cache to DB
        await SaveCandleCacheToDb();

        // 8. Save odds
        _logger.LogInformation("Saving: {H2H} h2h, {Spreads} spreads, {Totals} totals", h2hRecords.Count, spreadRecords.Count, totalRecords.Count);

        if (h2hRecords.Any())
            await _bookmakerOddsRepo.AddOrUpdateBookmakerOdds(h2hRecords);
        if (spreadRecords.Any())
            await _bookmakerOddsRepo.AddOrUpdateBookmakerSpreads(spreadRecords);
        if (totalRecords.Any())
            await _bookmakerOddsRepo.AddOrUpdateBookmakerTotals(totalRecords);
        await _bookmakerOddsRepo.Commit();

        _logger.LogInformation("Kalshi backfill complete.");
    }

    private enum MarketType { H2H, Spread, Total }

    private record EventMatch(int GameId, List<KalshiMarket> Markets);

    private Dictionary<string, EventMatch> MatchEventsToGames(
        List<KalshiMarket> markets, List<OddsApiResponseMapper.GameInfo> games, MarketType type)
    {
        var result = new Dictionary<string, EventMatch>();
        var byEvent = markets.GroupBy(m => m.EventTicker);

        foreach (var group in byEvent)
        {
            var marketList = group.ToList();
            var expiration = marketList[0].ExpectedExpirationTime;
            if (expiration == null) continue;
            var centralDate = KalshiResponseMapper.ToCentralDate(expiration.Value.ToUniversalTime());

            int? gameId = type switch
            {
                MarketType.H2H when marketList.Count == 2 =>
                    KalshiResponseMapper.MatchGameId(marketList, games, centralDate),
                MarketType.Spread when marketList.Count == 2 =>
                    KalshiResponseMapper.MatchGameIdFromNames(
                        marketList.Select(m => KalshiResponseMapper.ExtractSpreadTeamName(m.YesSubTitle)).ToList(),
                        games, centralDate),
                MarketType.Total =>
                    KalshiResponseMapper.MatchGameIdFromSingle(marketList[0], games, centralDate),
                _ => null
            };

            if (gameId != null)
                result[group.Key] = new EventMatch(gameId.Value, marketList);
        }

        return result;
    }

    // In-memory cache for candlestick results, keyed by market ticker.
    // Persists for the duration of the backfill run.
    private readonly Dictionary<string, KalshiCandlestick?> _candleCache = new();

    private async Task<KalshiCandlestick?> FetchGameDayCandle(string seriesTicker, string ticker, DateTime gameDateUtc)
    {
        if (_candleCache.TryGetValue(ticker, out var cached))
            return cached;

        var sixAmUtc = Get6amCentralUtc(gameDateUtc);
        var startTs = new DateTimeOffset(sixAmUtc).ToUnixTimeSeconds();
        var endTs = startTs + 3600; // 1 hour window

        var response = await _kalshiGetter.GetCandlesticks(seriesTicker, ticker, startTs, endTs, 60);
        if (response?.Candlesticks.Any() == true)
        {
            _candleCache[ticker] = response.Candlesticks.First();
            return _candleCache[ticker];
        }

        // If no data at 6am, try wider window (midnight to noon CT) and take the latest candle
        var midnightUtc = sixAmUtc.AddHours(-6);
        var noonUtc = sixAmUtc.AddHours(6);
        startTs = new DateTimeOffset(midnightUtc).ToUnixTimeSeconds();
        endTs = new DateTimeOffset(noonUtc).ToUnixTimeSeconds();

        response = await _kalshiGetter.GetCandlesticks(seriesTicker, ticker, startTs, endTs, 60);
        var candle = response?.Candlesticks.LastOrDefault();
        _candleCache[ticker] = candle;
        return candle;
    }

    private static DateTime Get6amCentralUtc(DateTime gameDateUtc)
    {
        var centralDate = TimeZoneInfo.ConvertTimeFromUtc(gameDateUtc, CentralZone).Date;
        var sixAmCentral = new DateTime(centralDate.Year, centralDate.Month, centralDate.Day, 6, 0, 0);
        return TimeZoneInfo.ConvertTimeToUtc(sixAmCentral, CentralZone);
    }

    private async Task LoadCandleCacheFromDb()
    {
        foreach (var key in new[] { "candles-KXNHLGAME", "candles-KXNHLSPREAD", "candles-KXNHLTOTAL" })
        {
            var sentinel = CacheSentinels[key];
            var cached = await _bookmakerOddsRepo.GetCachedResponse(sentinel);
            if (cached == null) continue;

            var dict = JsonConvert.DeserializeObject<Dictionary<string, KalshiCandlestick?>>(cached.RawJson);
            if (dict == null) continue;

            foreach (var (ticker, candle) in dict)
                _candleCache.TryAdd(ticker, candle);

            _logger.LogInformation("Loaded {Count} cached candlesticks from {Key}", dict.Count, key);
        }
    }

    private async Task SaveCandleCacheToDb()
    {
        // Split cache by series ticker prefix and save each as a separate cache entry
        var groups = new Dictionary<string, Dictionary<string, KalshiCandlestick?>>
        {
            ["candles-KXNHLGAME"] = new(),
            ["candles-KXNHLSPREAD"] = new(),
            ["candles-KXNHLTOTAL"] = new(),
        };

        foreach (var (ticker, candle) in _candleCache)
        {
            var key = ticker.StartsWith("KXNHLSPREAD") ? "candles-KXNHLSPREAD"
                    : ticker.StartsWith("KXNHLTOTAL") ? "candles-KXNHLTOTAL"
                    : "candles-KXNHLGAME";
            groups[key][ticker] = candle;
        }

        foreach (var (key, dict) in groups)
        {
            if (!dict.Any()) continue;
            var json = JsonConvert.SerializeObject(dict);
            await _bookmakerOddsRepo.SaveRawResponse(json, CacheSentinels[key]);
            _logger.LogInformation("Saved {Count} candlesticks to cache {Key}", dict.Count, key);
        }

        await _bookmakerOddsRepo.Commit();
    }

    private async Task<List<KalshiMarket>> GetOrFetchSettledMarkets(string sentinelKey, string seriesTicker)
    {
        var sentinelDate = CacheSentinels[sentinelKey];
        var cached = await _bookmakerOddsRepo.GetCachedResponse(sentinelDate);

        if (cached != null)
        {
            var cacheAge = DateTime.UtcNow - cached.FetchedDateUTC;
            if (cacheAge.TotalDays < 7)
            {
                _logger.LogInformation("Using cached settled markets for {Key} (age: {Days:F1} days)", sentinelKey, cacheAge.TotalDays);
                return JsonConvert.DeserializeObject<List<KalshiMarket>>(cached.RawJson) ?? new List<KalshiMarket>();
            }

            _logger.LogInformation("Cache for {Key} is {Days:F1} days old — re-fetching", sentinelKey, cacheAge.TotalDays);
        }

        _logger.LogInformation("Fetching settled markets for {Key}...", sentinelKey);
        var markets = await _kalshiGetter.GetSettledMarkets(seriesTicker);

        var json = JsonConvert.SerializeObject(markets);
        await _bookmakerOddsRepo.SaveRawResponse(json, sentinelDate);
        await _bookmakerOddsRepo.Commit();

        return markets;
    }
}
