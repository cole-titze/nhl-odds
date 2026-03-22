using System.Diagnostics;
using BookmakerOddsGetter;
using DatabaseAccess;
using DatabaseAccess.BookmakerOddsRepository;
using DatabaseAccess.BroadcasterRepository;
using DatabaseAccess.CleanedGameRepository;
using DatabaseAccess.ErrorRepository;
using DatabaseAccess.GameEventRepository;
using DatabaseAccess.GameEventSeasonRepository;
using DatabaseAccess.GameRepository;
using DatabaseAccess.GameSeasonRepository;
using DatabaseAccess.PlayerRepository;
using DatabaseAccess.PlayerStatsSeasonRepository;
using DatabaseAccess.TeamRepository;
using DataCleaner;
using DataGetter.BusinessLogic;
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

        if (modeSettings.Mode == ModeType.KalshiFetch)
        {
            var kalshiDbContext = new NhlDbContext(modeSettings.ConnectionString);
            var kalshiOddsRepo = new BookmakerOddsRepository(kalshiDbContext);
            var kalshiGetter = new KalshiGetter(_loggerFactory);
            var kalshiFetcher = new KalshiOddsFetcher(kalshiDbContext, kalshiOddsRepo, kalshiGetter, _loggerFactory);

            _logger.LogTrace("Starting Kalshi Odds Fetch");
            await kalshiFetcher.FetchAndSaveKalshiOdds();
            _logger.LogTrace("Completed Kalshi Odds Fetch");
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
        else
        {
            _logger.LogTrace("Starting Data Getter");
            await dataManager.GetNhlData(yearRange, modeSettings.Mode);
            _logger.LogTrace("Completed Data Getter");

            // Run data cleaner with a separate DbContext to avoid EF tracking conflicts
            var cleanerDbContext = new NhlDbContext(modeSettings.ConnectionString);
            var gameSeasonRepo = new GameSeasonRepository(cleanerDbContext);
            var cleanedGameRepo = new CleanedGameRepository(cleanerDbContext);
            var playerStatsRepo = new PlayerStatsSeasonRepository(cleanerDbContext);
            var cleanerErrorDbContext = new NhlDbContext(modeSettings.ConnectionString);
            var cleanerErrorRepo = new ErrorRepository(cleanerErrorDbContext);
            var gameEventSeasonRepo = new GameEventSeasonRepository(cleanerDbContext);
            var gameCleaner = new GameCleaner(gameSeasonRepo, cleanedGameRepo, playerStatsRepo, gameEventSeasonRepo, cleanerErrorRepo, _loggerFactory);

            _logger.LogTrace("Starting Data Cleaner");
            await gameCleaner.CleanGamesInSeasons(yearRange);
            _logger.LogTrace("Completed Data Cleaner");
        }

        watch.Stop();
        var elapsedTime = watch.Elapsed;
        var minutes = elapsedTime.TotalMinutes.ToString();
        _logger.LogTrace("Completed Data Collection in " + minutes + " minutes");
    }
}

