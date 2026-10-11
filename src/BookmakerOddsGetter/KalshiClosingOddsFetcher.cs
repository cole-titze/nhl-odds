using DatabaseAccess;
using DatabaseAccess.BookmakerOddsRepository;
using Entities.DbModels;
using Entities.ServiceModels.Kalshi;
using Entities.ServiceModels.Mappers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Services.Kalshi;

namespace BookmakerOddsGetter;

/// <summary>
/// Saves Kalshi's moneyline price at puck drop (the closing line) for played games, so picks can be graded on
/// closing-line value. Kalshi keeps price history, so this runs after the fact: the daily job covers the last
/// few days and the backfill covers every game with a Kalshi moneyline row.
/// </summary>
public class KalshiClosingOddsFetcher
{
    private const string BOOKMAKER_NAME = "Kalshi";
    private const string BOOKMAKER_KEY = "kalshi";
    private const string GAME_SERIES = "KXNHLGAME";
    private const int COMMIT_EVERY = 100;
    // How far back from the start to look for the last quote; a quiet market can go a while between candles
    private static readonly TimeSpan LookBack = TimeSpan.FromHours(3);

    private readonly NhlDbContext _dbContext;
    private readonly IBookmakerOddsRepository _bookmakerOddsRepo;
    private readonly IKalshiGetter _kalshiGetter;
    private readonly ILogger<KalshiClosingOddsFetcher> _logger;

    public KalshiClosingOddsFetcher(
        NhlDbContext dbContext,
        IBookmakerOddsRepository bookmakerOddsRepo,
        IKalshiGetter kalshiGetter,
        ILoggerFactory loggerFactory)
    {
        _dbContext = dbContext;
        _bookmakerOddsRepo = bookmakerOddsRepo;
        _kalshiGetter = kalshiGetter;
        _logger = loggerFactory.CreateLogger<KalshiClosingOddsFetcher>();
    }

    /// <param name="playedSinceUtc">Only games that started after this (null: every game)</param>
    public async Task FetchAndSaveClosingOdds(DateTime? playedSinceUtc = null)
    {
        var games = await (
                from g in _dbContext.GameRaw
                join o in _dbContext.BookmakerOdds on g.Id equals o.GameId
                where g.HasBeenPlayed
                    && o.BookmakerName == BOOKMAKER_NAME
                    && o.OddsApiGameId.StartsWith(GAME_SERIES)
                    && (playedSinceUtc == null || g.GameDateUTC >= playedSinceUtc)
                    && !_dbContext.BookmakerClosingOdds.Any(c => c.GameId == g.Id && c.BookmakerName == BOOKMAKER_NAME)
                orderby g.GameDateUTC
                select new { g.Id, g.HomeTeamId, g.AwayTeamId, g.SeasonStartYear, g.GameDateUTC, EventTicker = o.OddsApiGameId })
            .ToListAsync();

        if (!games.Any())
        {
            _logger.LogInformation("No played games need Kalshi closing odds.");
            return;
        }

        var seasonYears = games.Select(g => g.SeasonStartYear).Distinct().ToList();
        var teamNames = (await _dbContext.SeasonTeam
                .Where(t => seasonYears.Contains(t.SeasonStartYear))
                .ToListAsync())
            .ToDictionary(t => (t.SeasonStartYear, t.TeamId), t => t.Name);

        _logger.LogInformation("Fetching Kalshi closing odds for {Count} games...", games.Count);

        var pending = new List<DbBookmakerClosingOdds>();
        var saved = 0;
        foreach (var game in games)
        {
            if (!teamNames.TryGetValue((game.SeasonStartYear, game.HomeTeamId), out var homeName)
                || !teamNames.TryGetValue((game.SeasonStartYear, game.AwayTeamId), out var awayName))
                continue;

            var gameInfo = new OddsApiResponseMapper.GameInfo
            {
                GameId = game.Id,
                HomeTeamName = homeName,
                AwayTeamName = awayName,
                GameDateUTC = game.GameDateUTC,
            };
            var markets = await _kalshiGetter.GetEventMarkets(game.EventTicker);
            var (homeMarket, awayMarket) = KalshiResponseMapper.MatchHomeAway(markets, gameInfo);
            if (homeMarket == null || awayMarket == null)
            {
                _logger.LogWarning("No home/away Kalshi markets for game {GameId} ({Event})", game.Id, game.EventTicker);
                continue;
            }

            var homeCandle = await GetClosingCandle(homeMarket.Ticker, game.GameDateUTC);
            var awayCandle = await GetClosingCandle(awayMarket.Ticker, game.GameDateUTC);
            if (homeCandle == null || awayCandle == null)
            {
                _logger.LogWarning("No Kalshi quote before puck drop for game {GameId} ({Event})", game.Id, game.EventTicker);
                continue;
            }

            // Same price as the morning rows: yes_ask, the cost to buy each side (includes the vig)
            pending.Add(new DbBookmakerClosingOdds
            {
                GameId = game.Id,
                BookmakerName = BOOKMAKER_NAME,
                BookmakerKey = BOOKMAKER_KEY,
                HomeOdds = homeCandle.YesAsk.Close,
                AwayOdds = awayCandle.YesAsk.Close,
                PriceTimeUTC = DateTimeOffset.FromUnixTimeSeconds(
                    Math.Min(homeCandle.EndPeriodTs, awayCandle.EndPeriodTs)).UtcDateTime,
                FetchedDateUTC = DateTime.UtcNow,
                OddsApiGameId = game.EventTicker,
            });

            if (pending.Count >= COMMIT_EVERY)
                saved += await Save(pending);
        }
        saved += await Save(pending);

        _logger.LogInformation("Saved Kalshi closing odds for {Saved} of {Count} games", saved, games.Count);
    }

    private async Task<KalshiCandlestick?> GetClosingCandle(string ticker, DateTime startUtc)
    {
        var endTs = new DateTimeOffset(DateTime.SpecifyKind(startUtc, DateTimeKind.Utc)).ToUnixTimeSeconds();
        var startTs = endTs - (long)LookBack.TotalSeconds;
        var response = await _kalshiGetter.GetCandlesticks(GAME_SERIES, ticker, startTs, endTs, 1);
        return LastQuoteBefore(response?.Candlesticks ?? [], endTs);
    }

    /// <summary>The last one-minute candle that ends by the start and has an ask; Kalshi keeps trading in-game, so
    /// nothing after the start counts</summary>
    public static KalshiCandlestick? LastQuoteBefore(IEnumerable<KalshiCandlestick> candles, long startTs) =>
        candles
            .Where(c => c.EndPeriodTs <= startTs && c.YesAsk.Close > 0)
            .MaxBy(c => c.EndPeriodTs);

    private async Task<int> Save(List<DbBookmakerClosingOdds> pending)
    {
        if (!pending.Any())
            return 0;
        await _bookmakerOddsRepo.AddOrUpdateClosingOdds(pending);
        await _bookmakerOddsRepo.Commit();
        var count = pending.Count;
        pending.Clear();
        return count;
    }
}