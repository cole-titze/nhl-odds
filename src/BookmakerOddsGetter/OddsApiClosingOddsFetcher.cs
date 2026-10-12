using System.Text.Json;
using DatabaseAccess;
using DatabaseAccess.BookmakerOddsRepository;
using Entities.DbModels;
using Entities.ServiceModels.Mappers;
using Entities.ServiceModels.OddsApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Services.OddsApi;

namespace BookmakerOddsGetter;

/// <summary>
/// Saves every Odds API bookmaker's price just before puck drop (moneyline, spread, total) to the closing tables.
/// The live /odds endpoint drops finished games and shows in-game prices after the start, so the daily job runs
/// every few minutes and fetches games about to start. The free key's 500 credits a month don't cover all three
/// markets for every start time, so spread (then total) is skipped when the month's credits run short. The
/// backfill reads paid historical snapshots instead.
/// </summary>
public class OddsApiClosingOddsFetcher
{
    public const string ALL_MARKETS = "h2h,spreads,totals";
    private const string KALSHI_KEY = "kalshi";
    // Matches the CronJob's 5-minute schedule plus a minute for the pod to start
    private static readonly TimeSpan DueWindow = TimeSpan.FromMinutes(6);
    // The API's start time can be a few minutes later than the NHL's
    private static readonly TimeSpan CommenceSlack = TimeSpan.FromMinutes(30);
    // The 6 AM NextDayOdds run fetches all three markets
    private const int MORNING_CALL_CREDITS = 3;
    private static readonly TimeSpan MorningRunCentral = TimeSpan.FromHours(6);
    // Historical snapshots exist from this date
    private static readonly DateTime HistoryStartUtc = new(2020, 6, 6, 0, 0, 0, DateTimeKind.Utc);
    // Query the snapshot just before the start; the API returns the last one at or before it
    private static readonly TimeSpan BackfillLead = TimeSpan.FromMinutes(1);

    private static readonly (string Markets, int Cost)[] MarketTiers =
    [
        (ALL_MARKETS, 3),
        ("h2h,totals", 2),
        ("h2h", 1),
    ];

    private readonly NhlDbContext _dbContext;
    private readonly IBookmakerOddsRepository _bookmakerOddsRepo;
    private readonly IOddsApiGetter _oddsApiGetter;
    private readonly ILogger<OddsApiClosingOddsFetcher> _logger;

    public OddsApiClosingOddsFetcher(
        NhlDbContext dbContext,
        IBookmakerOddsRepository bookmakerOddsRepo,
        IOddsApiGetter oddsApiGetter,
        ILoggerFactory loggerFactory)
    {
        _dbContext = dbContext;
        _bookmakerOddsRepo = bookmakerOddsRepo;
        _oddsApiGetter = oddsApiGetter;
        _logger = loggerFactory.CreateLogger<OddsApiClosingOddsFetcher>();
    }

    /// <summary>Fetches closing odds for games starting within the next few minutes. Throws when there are
    /// games due but no credits to spare, so the miss lands in ErrorLog.</summary>
    public async Task FetchAndSaveDueClosingOdds()
    {
        var now = DateTime.UtcNow;
        var monthEnd = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);
        var upcoming = await _dbContext.GameRaw
            .Where(g => !g.HasBeenPlayed && g.GameDateUTC > now && g.GameDateUTC < monthEnd.AddDays(1))
            .Where(g => !_dbContext.BookmakerClosingOdds.Any(c => c.GameId == g.Id && c.BookmakerKey != KALSHI_KEY))
            .Select(g => new { g.Id, g.HomeTeamId, g.AwayTeamId, g.SeasonStartYear, g.GameDateUTC })
            .ToListAsync();

        var due = upcoming.Where(g => g.GameDateUTC < now + DueWindow).ToList();
        if (!due.Any())
        {
            _logger.LogInformation("No games start in the next {Minutes} minutes.", DueWindow.TotalMinutes);
            return;
        }

        var credits = await _oddsApiGetter.GetRemainingCredits()
            ?? throw new InvalidOperationException("Odds API did not report remaining credits");
        var reserve = MORNING_CALL_CREDITS * MorningCallsLeft(now, monthEnd, upcoming.Select(g => g.GameDateUTC));
        var slotsAfter = upcoming
            .Where(g => g.GameDateUTC >= now + DueWindow && g.GameDateUTC < monthEnd)
            .Select(g => g.GameDateUTC)
            .Distinct()
            .Count();
        var markets = ChooseMarkets(credits, reserve, slotsAfter);
        _logger.LogInformation("{Credits} credits left, {Reserve} held for the 6 AM runs, {Slots} later start times this month: fetching {Markets}",
            credits, reserve, slotsAfter, markets ?? "nothing");
        if (markets == null)
            throw new InvalidOperationException(
                $"No Odds API credits to spare for {due.Count} games starting soon ({credits} left, {reserve} held for the 6 AM runs)");

        var gameInfoList = await BuildGameInfoList(due.Select(g => (g.Id, g.HomeTeamId, g.AwayTeamId, g.SeasonStartYear, g.GameDateUTC)));
        var result = await _oddsApiGetter.GetOddsStartingBetween(markets, now, due.Max(g => g.GameDateUTC) + CommenceSlack);
        if (!result.Responses.Any())
        {
            _logger.LogWarning("The Odds API returned no games for {Count} games starting soon", due.Count);
            return;
        }

        await _bookmakerOddsRepo.SaveRawResponse(result.RawJson);
        var saved = await MapAndSave(result.Responses, gameInfoList, now, now);
        _logger.LogInformation("Saved Odds API closing odds for {Saved} of {Count} games", saved, due.Count);
    }

    /// <summary>Reads the historical snapshot just before every start time with played games missing an Odds API
    /// closing moneyline, spread or total (snapshots already read are free to read again). Raw responses are cached by query time, so a stopped run picks up where it left off.</summary>
    public async Task BackfillClosingOdds()
    {
        var now = DateTime.UtcNow;
        var games = await _dbContext.GameRaw
            .Where(g => g.HasBeenPlayed && g.GameDateUTC >= HistoryStartUtc && g.GameDateUTC < now)
            // Also games the daily job saved without spread or total to save credits
            .Where(g => !_dbContext.BookmakerClosingOdds.Any(c => c.GameId == g.Id && c.BookmakerKey != KALSHI_KEY)
                || !_dbContext.BookmakerClosingSpreads.Any(c => c.GameId == g.Id)
                || !_dbContext.BookmakerClosingTotals.Any(c => c.GameId == g.Id))
            .Select(g => new { g.Id, g.HomeTeamId, g.AwayTeamId, g.SeasonStartYear, g.GameDateUTC })
            .ToListAsync();
        // Newest first, so a credit plan that runs out covers the recent seasons
        var slots = games.GroupBy(g => g.GameDateUTC).OrderByDescending(s => s.Key).ToList();
        _logger.LogInformation("Backfilling Odds API closing odds: {Games} games, {Slots} start times", games.Count, slots.Count);

        var saved = 0;
        foreach (var slot in slots)
        {
            var queryDate = slot.Key - BackfillLead;
            var snapshot = await GetOrFetchSnapshot(queryDate);
            if (snapshot == null)
            {
                _logger.LogWarning("Stopping at {Start}: the historical request failed (progress is saved)", slot.Key);
                return;
            }

            var gameInfoList = await BuildGameInfoList(slot.Select(g => (g.Id, g.HomeTeamId, g.AwayTeamId, g.SeasonStartYear, g.GameDateUTC)));
            saved += await MapAndSave(snapshot.Data, gameInfoList, queryDate, snapshot.Timestamp ?? queryDate);
        }

        _logger.LogInformation("Backfill complete: closing odds for {Saved} of {Games} games", saved, games.Count);
    }

    /// <summary>The richest market set that leaves enough for every later start time this month at the same
    /// price, after holding back the 6 AM runs; moneyline alone while any credit is left; null when none is.</summary>
    public static string? ChooseMarkets(int creditsLeft, int reserve, int slotsAfterThis)
    {
        var spendable = creditsLeft - reserve;
        foreach (var (markets, cost) in MarketTiers)
            if (spendable >= cost * (slotsAfterThis + 1))
                return markets;
        return spendable >= 1 ? "h2h" : null;
    }

    /// <summary>6 AM Central runs left before the quota resets that will find a game to price</summary>
    public static int MorningCallsLeft(DateTime nowUtc, DateTime resetUtc, IEnumerable<DateTime> upcomingStartsUtc)
    {
        var central = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");
        var lastStart = upcomingStartsUtc.DefaultIfEmpty(DateTime.MinValue).Max();
        var count = 0;
        for (var day = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, central).Date; ; day = day.AddDays(1))
        {
            var runUtc = TimeZoneInfo.ConvertTimeToUtc(day + MorningRunCentral, central);
            if (runUtc >= resetUtc || runUtc >= lastStart)
                return count;
            if (runUtc > nowUtc)
                count++;
        }
    }

    public static (List<DbBookmakerClosingOdds> H2H, List<DbBookmakerClosingSpreads> Spreads, List<DbBookmakerClosingTotals> Totals)
        ToClosing(OddsApiResponseMapper.MappedOdds mapped, DateTime fallbackPriceTimeUtc, DateTime fetchedUtc)
    {
        var h2h = mapped.H2H
            .DistinctBy(o => (o.GameId, o.BookmakerName))
            .Select(o => new DbBookmakerClosingOdds
            {
                GameId = o.GameId,
                BookmakerName = o.BookmakerName,
                BookmakerKey = o.BookmakerKey,
                HomeOdds = o.HomeOdds,
                AwayOdds = o.AwayOdds,
                PriceTimeUTC = PriceTime(o.MarketLastUpdate, fallbackPriceTimeUtc),
                FetchedDateUTC = fetchedUtc,
                OddsApiGameId = o.OddsApiGameId,
            })
            .ToList();
        var spreads = mapped.Spreads
            .DistinctBy(o => (o.GameId, o.BookmakerName))
            .Select(o => new DbBookmakerClosingSpreads
            {
                GameId = o.GameId,
                BookmakerName = o.BookmakerName,
                BookmakerKey = o.BookmakerKey,
                HomePoint = o.HomePoint,
                HomePrice = o.HomePrice,
                AwayPoint = o.AwayPoint,
                AwayPrice = o.AwayPrice,
                PriceTimeUTC = PriceTime(o.MarketLastUpdate, fallbackPriceTimeUtc),
                FetchedDateUTC = fetchedUtc,
                OddsApiGameId = o.OddsApiGameId,
            })
            .ToList();
        var totals = mapped.Totals
            .DistinctBy(o => (o.GameId, o.BookmakerName))
            .Select(o => new DbBookmakerClosingTotals
            {
                GameId = o.GameId,
                BookmakerName = o.BookmakerName,
                BookmakerKey = o.BookmakerKey,
                OverUnderPoint = o.OverUnderPoint,
                OverPrice = o.OverPrice,
                UnderPrice = o.UnderPrice,
                PriceTimeUTC = PriceTime(o.MarketLastUpdate, fallbackPriceTimeUtc),
                FetchedDateUTC = fetchedUtc,
                OddsApiGameId = o.OddsApiGameId,
            })
            .ToList();
        return (h2h, spreads, totals);
    }

    /// <summary>When the book last moved the price (a suspended market stops updating); the fetch or snapshot time
    /// when the API didn't say</summary>
    private static DateTime PriceTime(DateTime marketLastUpdate, DateTime fallbackUtc) =>
        marketLastUpdate == default ? fallbackUtc : marketLastUpdate.ToUniversalTime();

    /// <returns>Games saved</returns>
    private async Task<int> MapAndSave(List<OddsApiResponse> responses, List<OddsApiResponseMapper.GameInfo> gameInfoList,
        DateTime asOfUtc, DateTime priceTimeUtc)
    {
        var mapped = OddsApiResponseMapper.Map(responses, gameInfoList, asOfUtc);
        var (h2h, spreads, totals) = ToClosing(mapped, priceTimeUtc, DateTime.UtcNow);

        if (h2h.Any())
            await _bookmakerOddsRepo.AddOrUpdateClosingOdds(h2h);
        if (spreads.Any())
            await _bookmakerOddsRepo.AddOrUpdateClosingSpreads(spreads);
        if (totals.Any())
            await _bookmakerOddsRepo.AddOrUpdateClosingTotals(totals);
        await _bookmakerOddsRepo.Commit();

        var games = h2h.Select(o => o.GameId).Distinct().Count();
        _logger.LogInformation("  {Games} of {Expected} games matched: {H2H} h2h, {Spreads} spreads, {Totals} totals rows",
            games, gameInfoList.Count, h2h.Count, spreads.Count, totals.Count);
        return games;
    }

    private async Task<OddsApiHistoricalResponse?> GetOrFetchSnapshot(DateTime queryDate)
    {
        var cached = await _bookmakerOddsRepo.GetCachedResponse(queryDate);
        if (cached != null)
            return JsonSerializer.Deserialize<OddsApiHistoricalResponse>(cached.RawJson) ?? new OddsApiHistoricalResponse();

        var result = await _oddsApiGetter.GetHistoricalOdds(queryDate);
        if (string.IsNullOrEmpty(result.RawJson))
            return null;

        _logger.LogInformation("Fetched snapshot for {Date} ({Remaining} credits left)", queryDate, _oddsApiGetter.GetRemainingRequests());
        // Cache even empty snapshots so they aren't paid for twice
        await _bookmakerOddsRepo.SaveRawResponse(result.RawJson, queryDate);
        await _bookmakerOddsRepo.Commit();
        return JsonSerializer.Deserialize<OddsApiHistoricalResponse>(result.RawJson) ?? new OddsApiHistoricalResponse();
    }

    private async Task<List<OddsApiResponseMapper.GameInfo>> BuildGameInfoList(
        IEnumerable<(int Id, int HomeTeamId, int AwayTeamId, int SeasonStartYear, DateTime GameDateUTC)> games)
    {
        var gameList = games.ToList();
        var seasons = gameList.Select(g => g.SeasonStartYear).Distinct().ToList();
        var teamNames = (await _dbContext.SeasonTeam
                .Where(t => seasons.Contains(t.SeasonStartYear))
                .ToListAsync())
            .ToDictionary(t => (t.SeasonStartYear, t.TeamId), t => t.Name);

        var gameInfoList = new List<OddsApiResponseMapper.GameInfo>();
        foreach (var game in gameList)
        {
            if (teamNames.TryGetValue((game.SeasonStartYear, game.HomeTeamId), out var homeName)
                && teamNames.TryGetValue((game.SeasonStartYear, game.AwayTeamId), out var awayName))
                gameInfoList.Add(new OddsApiResponseMapper.GameInfo
                {
                    GameId = game.Id,
                    HomeTeamName = homeName,
                    AwayTeamName = awayName,
                    GameDateUTC = game.GameDateUTC,
                });
        }
        return gameInfoList;
    }
}
