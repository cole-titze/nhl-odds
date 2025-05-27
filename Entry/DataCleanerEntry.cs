using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Entities.Types;
using DatabaseAccess;
using DatabaseAccess.GameRepository;
using DatabaseAccess.PlayerRepository;
using DatabaseAccess.CleanedGameRepository;
using DataCleaner.BusinessLogic.GameCleaner;

namespace Entry
{
    public class DataCleanerEntry
    {
        private const int START_YEAR = 2010;
        private readonly ILogger<DataCleanerEntry> _logger;
        private readonly ILoggerFactory _loggerFactory;

        public DataCleanerEntry(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<DataCleanerEntry>();
        }
        /// <summary>
        /// Gets and cleans all new games and player values. Stores CleanedGames in db.
        /// </summary>
        /// <param name="gamesConnectionString">db connection string</param>
        /// <returns>None</returns>
        public async Task Main(string gamesConnectionString)
        {
            var watch = Stopwatch.StartNew();
            Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));

            var nhlDbContext = new NhlDbContext(gamesConnectionString);
            var playerRepo = new PlayerRepository(nhlDbContext);
            var cleanedGameRepo = new CleanedGameRepository(nhlDbContext);
            var gameRepo = new GameRepository(nhlDbContext, _loggerFactory);
            var yearRange = new YearRange(START_YEAR, DateTime.Now);

            _logger.LogTrace("Starting Game Cleaning");
            var gameCleaner = new GameCleaner(gameRepo, cleanedGameRepo, playerRepo, _loggerFactory);
            await gameCleaner.CleanGamesInSeasons(yearRange);

            watch.Stop();
            var elapsedTime = watch.Elapsed;
            var minutes = elapsedTime.TotalMinutes.ToString();
            _logger.LogTrace("Completed Game Cleaner in " + minutes + " minutes");
        }
    }
}

