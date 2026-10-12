using System.Diagnostics;
using BookmakerOddsGetter;
using DatabaseAccess;
using DatabaseAccess.BookmakerOddsRepository;
using DatabaseAccess.BroadcasterRepository;
using DatabaseAccess.ErrorRepository;
using DatabaseAccess.GameDetailRepository;
using DatabaseAccess.GameEventRepository;
using DatabaseAccess.GameRepository;
using DatabaseAccess.LineupArticleRepository;
using DatabaseAccess.PlayerRepository;
using DatabaseAccess.RawResponseRepository;
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
    // NextDayKalshi saves closing odds for games played in this many past days (a missed morning is caught up)
    private const int CLOSING_ODDS_DAYS = 3;
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
        var rawResponses = new RawResponseCache(
            new RawResponseRepository(() => new NhlDbContext(modeSettings.ConnectionString)), RawPolicy(modeSettings), _loggerFactory);
        var requestMaker = new RequestMaker(new HttpClientWrapper(), _loggerFactory, modeSettings.ThrottleTimeMs, rawResponses);

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
        else if (modeSettings.Mode == ModeType.NextDayKalshi)
        {
            var kalshiDbContext = new NhlDbContext(modeSettings.ConnectionString);
            var kalshiOddsRepo = new BookmakerOddsRepository(kalshiDbContext);
            var kalshiGetter = new KalshiGetter(_loggerFactory);
            var kalshiFetcher = new KalshiOddsFetcher(kalshiDbContext, kalshiOddsRepo, kalshiGetter, _loggerFactory);

            var closingFetcher = new KalshiClosingOddsFetcher(kalshiDbContext, kalshiOddsRepo, kalshiGetter, _loggerFactory);

            _logger.LogTrace("Starting Kalshi Odds Fetch");
            await LogFailure(errorRepo, "NextDayKalshi", kalshiFetcher.FetchAndSaveKalshiOdds);
            // Separate step so a closing-line hiccup never costs the morning odds the predictor needs
            await LogFailure(errorRepo, "NextDayKalshiClosing",
                () => closingFetcher.FetchAndSaveClosingOdds(DateTime.UtcNow.AddDays(-CLOSING_ODDS_DAYS)));
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
            await new KalshiClosingOddsFetcher(kalshiDbContext, kalshiOddsRepo, kalshiGetter, _loggerFactory)
                .FetchAndSaveClosingOdds();
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
                var bookmakerFetcher = new BookmakerOddsFetcher(oddsDbContext, bookmakerOddsRepo, oddsApiGetter, _loggerFactory);
                _logger.LogTrace("Starting Bookmaker Odds Getter");
                await LogFailure(errorRepo, "NextDayOdds", bookmakerFetcher.FetchAndSaveBookmakerOdds);
                _logger.LogTrace("Completed Bookmaker Odds Getter");
            }
        }
        else if (modeSettings.Mode == ModeType.ClosingOdds || modeSettings.Mode == ModeType.BackfillClosingOdds)
        {
            var isBackfill = modeSettings.Mode == ModeType.BackfillClosingOdds;
            var apiKey = isBackfill ? modeSettings.OddsApiBackfillKey : modeSettings.OddsApiKey;

            if (string.IsNullOrEmpty(apiKey))
                throw new Exception(isBackfill
                    ? "API_BACKFILL_KEY must be set for BackfillClosingOdds mode"
                    : "ODDS_API_KEY must be set for ClosingOdds mode");

            var oddsDbContext = new NhlDbContext(modeSettings.ConnectionString);
            var closingFetcher = new OddsApiClosingOddsFetcher(oddsDbContext, new BookmakerOddsRepository(oddsDbContext),
                new OddsApiGetter(apiKey, _loggerFactory), _loggerFactory);

            if (isBackfill)
                await closingFetcher.BackfillClosingOdds();
            else
                await LogFailure(errorRepo, "ClosingOdds", closingFetcher.FetchAndSaveDueClosingOdds);
        }
        else if (modeSettings.Mode == ModeType.NhlUpdate)
        {
            await UpdateGames(modeSettings, dataManager, rawResponses);
        }
        else
        {
            var cleanAll = modeSettings.Mode == ModeType.CleanAll;
            if (!cleanAll)
            {
                _logger.LogTrace("Starting Data Getter");
                await dataManager.GetNhlData(yearRange, modeSettings.Mode);
                _logger.LogTrace("Completed Data Getter");

                if (modeSettings.RefetchDays > 0)
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

            // Rosters, shift charts and goal replays aren't model inputs, so they come after cleaning
            if (!cleanAll)
            {
                try
                {
                    var recentGameIds = modeSettings.RefetchDays > 0
                        ? await gameRepo.GetPlayedGameIdsSince(DateTime.UtcNow.AddDays(-modeSettings.RefetchDays))
                        : [];
                    await CreateGameDetailManager(modeSettings, rawResponses).FetchRecent(yearRange.EndYear, recentGameIds);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to fetch game details");
                    await errorRepo.AddError(new DbErrorLog
                    {
                        TimestampUTC = DateTime.UtcNow,
                        ExceptionType = ex.GetType().FullName ?? ex.GetType().Name,
                        Message = ex.Message,
                        StackTrace = ex.StackTrace ?? string.Empty,
                        Source = "GameDetails",
                    });
                }
            }
        }

        rawResponses.LogTotals();
        watch.Stop();
        var elapsedTime = watch.Elapsed;
        var minutes = elapsedTime.TotalMinutes.ToString();
        _logger.LogTrace("Completed Data Collection in " + minutes + " minutes");
    }

    /// <summary>
    /// Runs a step and, if it throws, records the error in ErrorLog before rethrowing so the job still fails
    /// </summary>
    private static async Task LogFailure(ErrorRepository errorRepo, string source, Func<Task> step)
    {
        try
        {
            await step();
        }
        catch (Exception ex)
        {
            await errorRepo.AddError(new DbErrorLog
            {
                TimestampUTC = DateTime.UtcNow,
                ExceptionType = ex.GetType().FullName ?? ex.GetType().Name,
                Message = ex.Message,
                StackTrace = ex.StackTrace ?? string.Empty,
                Source = source,
            });
            throw;
        }
    }

    private GameDetailManager CreateGameDetailManager(ModeSettings modeSettings, RawResponseCache rawResponses)
    {
        return new GameDetailManager(
            () => new GameDetailRepository(new NhlDbContext(modeSettings.ConnectionString)),
            () => new ErrorRepository(new NhlDbContext(modeSettings.ConnectionString)),
            new NhlGameDetailGetter(rawResponses), _loggerFactory);
    }

    /// <summary>
    /// Re-fetches and overwrites games (the BACKFILL_GAME_IDS ones, else the SEASON_START-SEASON_END seasons), their
    /// rosters, shift charts and goal replays, then re-cleans the seasons they touch. With FROM_STORED it rebuilds
    /// from the stored raw responses instead of asking the API.
    /// </summary>
    private async Task UpdateGames(ModeSettings modeSettings, NhlDataManager dataManager, RawResponseCache rawResponses)
    {
        var onlyGameIds = modeSettings.BackfillGameIds.Distinct().Order().ToList();
        var range = onlyGameIds.Any()
            ? new YearRange(onlyGameIds[0] / 1_000_000, onlyGameIds[^1] / 1_000_000)
            : new YearRange(modeSettings.SeasonStart ?? START_YEAR,
                modeSettings.SeasonEnd ?? new YearRange(START_YEAR, DateTime.Now).EndYear);
        _logger.LogTrace("Starting Update of seasons {Start}-{End}{From}", range.StartYear, range.EndYear,
            modeSettings.FromStored ? " from stored responses" : string.Empty);

        if (onlyGameIds.Any() || modeSettings.FromStored)
            await dataManager.BackfillGames(onlyGameIds.Any() ? onlyGameIds : await PlayedGames(modeSettings, range));
        else
            await dataManager.GetNhlData(range, ModeType.NhlUpdate);

        // A season's features read the previous season too, so the season after the last one changes as well
        var currentSeason = new YearRange(START_YEAR, DateTime.Now).EndYear;
        var gameCleaner = new GameCleaner(() => new NhlDbContext(modeSettings.ConnectionString), _loggerFactory,
            modeSettings.CleanParallelism);
        await gameCleaner.CleanGamesInSeasons(
            new YearRange(range.StartYear, Math.Min(range.EndYear + 1, currentSeason)), cleanAll: true);

        // Rosters, shift charts and goal replays aren't model inputs, so they come after cleaning
        await CreateGameDetailManager(modeSettings, rawResponses).Refetch(
            onlyGameIds.Any() ? onlyGameIds : await PlayedGames(modeSettings, range));
        _logger.LogTrace("Completed Update");
    }

    /// <summary>
    /// FROM_STORED runs only read stored responses; everything else asks the API. All of them store what the API returns.
    /// </summary>
    private static RawResponsePolicy RawPolicy(ModeSettings modeSettings) =>
        modeSettings.FromStored ? RawResponsePolicy.StoredOnly : RawResponsePolicy.Fresh;

    private static async Task<List<int>> PlayedGames(ModeSettings modeSettings, YearRange seasons)
    {
        var repo = new GameDetailRepository(new NhlDbContext(modeSettings.ConnectionString));
        var gameIds = new List<int>();
        for (var season = seasons.StartYear; season <= seasons.EndYear; season++)
            gameIds.AddRange(await repo.GetPlayedGames(season));
        return gameIds;
    }

    /// <summary>
    /// Saves NHL.com content that only shows its current state: the lineup projections article (and its parsed
    /// lineups), the betting-partner odds widget and articles. Each step runs even if another fails; failures go to ErrorLog and fail the job at the end.
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
            new LineupArticleRepository(nhlDbContext), contentGetter, _loggerFactory);
        await Step("LineupSnapshot", () => lineupManager.SaveLineupArticle(now));
        var lineupParseManager = new NhlLineupParseManager(new LineupArticleRepository(nhlDbContext), contentGetter, _loggerFactory);
        await Step("LineupParse", async () =>
            failures.AddRange((await lineupParseManager.ParseNewVersions(now)).Select(f => ($"LineupParse {f.ArticleHash}", f.Error))));

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