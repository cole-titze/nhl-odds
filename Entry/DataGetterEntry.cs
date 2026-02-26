using System.Diagnostics;
using DatabaseAccess;
using DatabaseAccess.BroadcasterRepository;
using DatabaseAccess.ErrorRepository;
using DatabaseAccess.GameEventRepository;
using DatabaseAccess.GameRepository;
using DatabaseAccess.PlayerRepository;
using DatabaseAccess.TeamRepository;
using DataGetter.BusinessLogic;
using Entities.Types;
using Microsoft.Extensions.Logging;
using Services.NhlData;
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

        _logger.LogTrace("Starting Data Getter");
        await dataManager.GetNhlData(yearRange, modeSettings.Mode);
        _logger.LogTrace("Completed Data Getter");

        watch.Stop();
        var elapsedTime = watch.Elapsed;
        var minutes = elapsedTime.TotalMinutes.ToString();
        _logger.LogTrace("Completed Data Collection in " + minutes + " minutes");
    }
}

