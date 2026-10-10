using System.Diagnostics;
using BookmakerOddsGetter;
using DatabaseAccess;
using DatabaseAccess.BookmakerOddsRepository;
using DatabaseAccess.BroadcasterRepository;
using DatabaseAccess.ErrorRepository;
using DatabaseAccess.GameEventRepository;
using DatabaseAccess.GameRepository;
using DatabaseAccess.LineupArticleRepository;
using DatabaseAccess.PlayerRepository;
using DatabaseAccess.RosterStatusRepository;
using DatabaseAccess.SnapshotRepository;
using DatabaseAccess.TeamRepository;
using DataCleaner;
using DataGetter.BusinessLogic;
using Entities.DbModels;
using Entities.Types;
using Entities.Types.Enums;
using Microsoft.Extensions.Logging;
using Services.Kalshi;
using Services.NhlData;
using Services.OddsApi;
using Services.RequestMaker;

namespace Entry;

public class DataGetterEntry
{
    // 2009 was the first year with modern play-by-play statistics
    private const int START_YEAR = 2009;
    private readonly ILogger<DataGetterEntry> _logger;
    private readonly ILoggerFactory _loggerFactory;

    public DataGetterEntry(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<DataGetterEntry>();
        _loggerFactory = loggerFactory;
    }
    /// <summary>
    /// Gets and stores all new games and player values.
    /// </summary>
    /// <param name="modeSettings">db connection string and mode</param>
    /// <returns>None</returns>
    public async Task Main(ModeSettings modeSettings)
    {
        var watch = Stopwatch.StartNew();
        Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));

        var nhlDbContext = new NhlDbContext(modeSettings.ConnectionString);
        var playerRepo = new PlayerRepository(nhlDbContext);
        var gameEventRepo = new GameEventRepository(nhlDbContext);
        var gameRepo = new GameRepository(nhlDbContext, gameEventRepo);
        var broadcasterRepo = new BroadcasterRepository(nhlDbContext);
        var teamRepo = new TeamRepository(nhlDbContext);
        var errorDbContext = new NhlDbContext(modeSettings.ConnectionString);
        var errorRepo = new ErrorRepository(errorDbContext);
        var requestMaker = new RequestMaker(new HttpClientWrapper(), _loggerFactory, modeSettings.ThrottleTimeMs);

        var seasonGameCountCache = await gameRepo.GetSeasonGameCounts();
        INhlGameGetter gameDataGetter = new NhlApiGameGetter(requestMaker, _loggerFactory);
        INhlScheduleGetter scheduleDataGetter = new NhlApiScheduleGetter(requestMaker, seasonGameCountCache, _loggerFactory);
        INhlPlayerGetter playerDataGetter = new NhlApiPlayerGetter(requestMaker, _loggerFactory);

        var nhlRequestMaker = new NhlApiDataGetter(gameDataGetter, playerDataGetter, scheduleDataGetter);
        var yearRange = new YearRange(START_YEAR, DateTime.Now);

        var teamGetter = new NhlTeamManager(teamRepo, nhlRequestMaker, _loggerFactory);
        var gameGetter = new NhlGameManager(gameRepo, playerRepo, nhlRequestMaker, _loggerFactory);
        var playerGetter = new NhlPlayerManager(playerRepo, nhlRequestMaker, _loggerFactory);

        var dataManager = new NhlDataManager(gameRepo, playerRepo, teamRepo, errorRepo, broadcasterRepo, gameEventRepo, gameGetter, playerGetter, teamGetter, _loggerFactory);

        if (modeSettings.Mode == ModeType.LineupSnapshot)
        {
            await SaveSnapshots(nhlDbContext, errorRepo);
        }
        else if (modeSettings.Mode == ModeType.KalshiFetch)
        {
            var kalshiDbContext = new NhlDbContext(modeSettings.ConnectionString);
            var kalshiOddsRepo = new BookmakerOddsRepository(kalshiDbContext);
            var kalshiGetter = new KalshiGetter(_loggerFactory);
            var kalshiFetcher = new KalshiOddsFetcher(kalshiDbContext, kalshiOddsRepo, kalshiGetter, _loggerFactory);

            _logger.LogTrace("Starting Kalshi Odds Fetch");
            await kalshiFetcher.FetchAndSaveKalshiOdds();
            _logger.LogTrace("Completed Kalshi Odds Fetch");
        }
        else if (modeSettings.Mode == ModeType.BackfillKalshi)
        {
            var kalshiDbContext = new NhlDbContext(modeSettings.ConnectionString);
            var kalshiOddsRepo = new BookmakerOddsRepository(kalshiDbContext);
            var kalshiGetter = new KalshiGetter(_loggerFactory);
            var kalshiBackfiller = new KalshiOddsBackfiller(kalshiDbContext, kalshiOddsRepo, kalshiGetter, _loggerFactory);

            _logger.LogTrace("Starting Kalshi Odds Backfill");
            await kalshiBackfiller.BackfillKalshiOdds();
            _logger.LogTrace("Completed Kalshi Odds Backfill");
        }
        else if (modeSettings.Mode == ModeType.NextDayOdds || modeSettings.Mode == ModeType.BackfillOdds)
        {
            var isBackfill = modeSettings.Mode == ModeType.BackfillOdds;
            var apiKey = isBackfill ? modeSettings.OddsApiBackfillKey : modeSettings.OddsApiKey;

            if (string.IsNullOrEmpty(apiKey))
                throw new Exception(isBackfill
                    ? "API_BACKFILL_KEY must be set for BackfillOdds mode"
                    : "ODDS_API_KEY must be set for NextDayOdds mode");

            var oddsDbContext = new NhlDbContext(modeSettings.ConnectionString);
            var bookmakerOddsRepo = new BookmakerOddsRepository(oddsDbContext);
            var oddsApiGetter = new OddsApiGetter(apiKey, _loggerFactory);

            if (isBackfill)
            {
                var backfiller = new BookmakerOddsBackfiller(oddsDbContext, bookmakerOddsRepo, oddsApiGetter, _loggerFactory);
                _logger.LogTrace("Starting Bookmaker Odds Backfill");
                await backfiller.BackfillBookmakerOdds();
                _logger.LogTrace("Completed Bookmaker Odds Backfill");
            }
            else
            {
                var kalshiGetter = new KalshiGetter(_loggerFactory);

                var bookmakerFetcher = new BookmakerOddsFetcher(oddsDbContext, bookmakerOddsRepo, oddsApiGetter, _loggerFactory, kalshiGetter);
                _logger.LogTrace("Starting Bookmaker Odds Getter");
                await bookmakerFetcher.FetchAndSaveBookmakerOdds();
                _logger.LogTrace("Completed Bookmaker Odds Getter");
            }
        }
        else if (modeSettings.Mode == ModeType.BackfillGame)
        {
            if (!modeSettings.BackfillGameIds.Any())
                throw new Exception("BACKFILL_GAME_IDS must be set for BackfillGame mode (comma-separated list of game IDs)");

            _logger.LogTrace("Starting Game Backfill for {Count} game(s)", modeSettings.BackfillGameIds.Count());
            await dataManager.BackfillGames(modeSettings.BackfillGameIds);
            _logger.LogTrace("Completed Game Backfill");
        }
        else if (modeSettings.Mode == ModeType.BackfillGoalieStats)
        {
            _logger.LogTrace("Starting Goalie Stats Backfill");
            await dataManager.BackfillGoalieShortHandedStats(yearRange);
            _logger.LogTrace("Completed Goalie Stats Backfill");
        }
        else
        {
            var cleanAll = modeSettings.Mode == ModeType.CleanAll;
            if (!cleanAll)
            {
                _logger.LogTrace("Starting Data Getter");
                await dataManager.GetNhlData(yearRange, modeSettings.Mode);
                _logger.LogTrace("Completed Data Getter");

                // NhlUpdate already re-fetched everything
                if (modeSettings.Mode == ModeType.NhlAdd && modeSettings.RefetchDays > 0)
                    await dataManager.RefetchRecentGames(modeSettings.RefetchDays);

                // Snapshot who is on injured reserve before cleaning, so today's projected lineups can use it
                try
                {
                    var rosterStatusDbContext = new NhlDbContext(modeSettings.ConnectionString);
                    var rosterStatusManager = new NhlRosterStatusManager(
                        new RosterStatusRepository(rosterStatusDbContext), new NhlRosterReportGetter(_loggerFactory), _loggerFactory);
                    await rosterStatusManager.SaveRosterSnapshot(DateTime.UtcNow);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save roster snapshot; cleaning without it");
                }
            }

            // The cleaner makes its own DbContexts (one per season) to avoid EF tracking conflicts
            var gameCleaner = new GameCleaner(() => new NhlDbContext(modeSettings.ConnectionString), _loggerFactory,
                modeSettings.CleanParallelism);

            _logger.LogTrace("Starting Data Cleaner");
            await gameCleaner.CleanGamesInSeasons(yearRange, cleanAll);
            _logger.LogTrace("Completed Data Cleaner");
        }

        watch.Stop();
        var elapsedTime = watch.Elapsed;
        var minutes = elapsedTime.TotalMinutes.ToString();
        _logger.LogTrace("Completed Data Collection in " + minutes + " minutes");
    }

    /// <summary>
    /// Saves NHL.com content that only shows its current state: the lineup projections article, the betting-partner
    /// odds widget and articles. Each step runs even if another fails; failures go to ErrorLog and fail the job at the end.
    /// </summary>
    private async Task SaveSnapshots(NhlDbContext nhlDbContext, ErrorRepository errorRepo)
    {
        var now = DateTime.UtcNow;
        var contentGetter = new NhlContentGetter();
        var snapshotRepo = new SnapshotRepository(nhlDbContext);
        var failures = new List<(string Source, Exception Error)>();

        async Task Step(string source, Func<Task> step)
        {
            try
            {
                await step();
            }
            catch (Exception ex)
            {
                failures.Add((source, ex));
            }
        }

        var lineupManager = new NhlLineupArticleManager(
            new LineupArticleRepository(nhlDbContext), new NhlLineupArticleGetter(_loggerFactory), _loggerFactory);
        await Step("LineupSnapshot", () => lineupManager.SaveLineupArticle(now));

        var oddsManager = new NhlPartnerOddsManager(snapshotRepo, contentGetter, _loggerFactory);
        foreach (var country in NhlPartnerOddsManager.Countries)
            await Step($"PartnerOdds {country}", () => oddsManager.SavePartnerOdds(country, now));

        var articleManager = new NhlArticleManager(snapshotRepo, contentGetter, _loggerFactory);
        await Step("RollingArticles", async () =>
            failures.AddRange((await articleManager.SaveRollingArticles(now)).Select(f => ($"RollingArticles {f.Url}", f.Error))));
        await Step("LatestArticles", async () =>
            failures.AddRange((await articleManager.SaveLatestArticles(now)).Select(f => ($"LatestArticles {f.Url}", f.Error))));

        if (failures.Count == 0)
            return;
        foreach (var (source, error) in failures)
        {
            _logger.LogError(error, "Snapshot step {Source} failed", source);
            await errorRepo.AddError(new DbErrorLog
            {
                TimestampUTC = DateTime.UtcNow,
                ExceptionType = error.GetType().FullName ?? error.GetType().Name,
                Message = error.Message,
                StackTrace = error.StackTrace ?? string.Empty,
                Source = source.Length > 250 ? source[..250] : source,
            });
        }
        throw new Exception($"{failures.Count} snapshot step(s) failed: {string.Join(", ", failures.Select(f => f.Source))}");
    }
}