using DatabaseAccess.CleanedGameRepository;
using DatabaseAccess.ErrorRepository;
using DatabaseAccess.GameEventSeasonRepository;
using DatabaseAccess.GameSeasonRepository;
using DatabaseAccess.PlayerStatsSeasonRepository;
using DataCleaner.Mappers;
using Entities.DbModels;
using Entities.Models;
using Entities.Types;
using Microsoft.Extensions.Logging;

namespace DataCleaner;

public class GameCleaner
{
    private readonly IGameSeasonRepository _gameRepo;
    private readonly ICleanedGameRepository _cleanedGameRepo;
    private readonly IPlayerStatsSeasonRepository _playerStatsRepo;
    private readonly IGameEventSeasonRepository _gameEventSeasonRepo;
    private readonly IErrorRepository _errorRepo;
    private readonly ILogger<GameCleaner> _logger;

    public GameCleaner(IGameSeasonRepository gameRepository, ICleanedGameRepository cleanedGameRepository,
        IPlayerStatsSeasonRepository playerStatsRepository, IGameEventSeasonRepository gameEventSeasonRepository,
        IErrorRepository errorRepository, ILoggerFactory loggerFactory)
    {
        _gameRepo = gameRepository;
        _cleanedGameRepo = cleanedGameRepository;
        _playerStatsRepo = playerStatsRepository;
        _gameEventSeasonRepo = gameEventSeasonRepository;
        _errorRepo = errorRepository;
        _logger = loggerFactory.CreateLogger<GameCleaner>();
    }

    public async Task CleanGamesInSeasons(YearRange seasonYearRange)
    {
        for (int seasonStartYear = seasonYearRange.StartYear; seasonStartYear <= seasonYearRange.EndYear; seasonStartYear++)
        {
            var seasonGames = await _gameRepo.GetSeasonGames(seasonStartYear);
            var gamesToClean = seasonGames;
            var existingCleanedGames = await _cleanedGameRepo.GetSeasonOfCleanedGames(seasonStartYear);

            if (seasonStartYear != seasonYearRange.EndYear)
                gamesToClean = GetGamesToClean(existingCleanedGames, seasonGames);

            if (!gamesToClean.Any())
            {
                _logger.LogInformation("All game data for season " + seasonStartYear.ToString() + " already exists. Skipping...");
                continue;
            }

            var lastSeasonGames = await _gameRepo.GetSeasonGames(seasonStartYear - 1);
            var gameMap = new SeasonGames(seasonGames.Concat(lastSeasonGames));

            // Load player stats for current + previous season to build roster scorer
            var currentSkaterStats = await _playerStatsRepo.GetSeasonSkaterStats(seasonStartYear);
            var lastSkaterStats = await _playerStatsRepo.GetSeasonSkaterStats(seasonStartYear - 1);
            var currentGoalieStats = await _playerStatsRepo.GetSeasonGoalieStats(seasonStartYear);
            var lastGoalieStats = await _playerStatsRepo.GetSeasonGoalieStats(seasonStartYear - 1);

            // Build game list from the raw games for date lookup
            var allGamesForScorer = seasonGames.Concat(lastSeasonGames)
                .Select(g => new DbGameRaw { Id = g.Id, GameDateUTC = g.GameDateUTC });

            var rosterScorer = new RosterScorer(
                currentSkaterStats.Concat(lastSkaterStats),
                currentGoalieStats.Concat(lastGoalieStats),
                allGamesForScorer);

            // Load event data for current + previous season to build event aggregator
            var currentPenalties = await _gameEventSeasonRepo.GetSeasonPenalties(seasonStartYear);
            var lastPenalties = await _gameEventSeasonRepo.GetSeasonPenalties(seasonStartYear - 1);
            var currentGoals = await _gameEventSeasonRepo.GetSeasonGoals(seasonStartYear);
            var lastGoals = await _gameEventSeasonRepo.GetSeasonGoals(seasonStartYear - 1);
            var currentFaceoffs = await _gameEventSeasonRepo.GetSeasonFaceoffs(seasonStartYear);
            var lastFaceoffs = await _gameEventSeasonRepo.GetSeasonFaceoffs(seasonStartYear - 1);
            var currentMissedShots = await _gameEventSeasonRepo.GetSeasonMissedShots(seasonStartYear);
            var lastMissedShots = await _gameEventSeasonRepo.GetSeasonMissedShots(seasonStartYear - 1);

            var eventAggregator = new EventAggregator(
                currentPenalties.Concat(lastPenalties),
                currentGoals.Concat(lastGoals),
                currentFaceoffs.Concat(lastFaceoffs),
                currentMissedShots.Concat(lastMissedShots),
                seasonGames.Concat(lastSeasonGames));

            const int batchSize = 200;
            var cleanedGames = new List<DbGameCleaned>();
            int totalCleaned = 0;
            foreach (var game in gamesToClean)
            {
                try
                {
                    var cleanedGame = MapGameToDbGameCleaned.Map(game, gameMap, rosterScorer);
                    MapEventToDbGameCleaned.Apply(cleanedGame, eventAggregator, game);
                    cleanedGames.Add(cleanedGame);

                    if (cleanedGames.Count >= batchSize)
                    {
                        await _cleanedGameRepo.AddUpdateCleanedGames(cleanedGames);
                        await _cleanedGameRepo.Commit();
                        totalCleaned += cleanedGames.Count;
                        cleanedGames.Clear();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error cleaning game {GameId} in season {Season}. Skipping.", game.Id, seasonStartYear);
                    cleanedGames.Clear();
                    _cleanedGameRepo.ClearTracking();
                    var stackTrace = ex.StackTrace ?? string.Empty;
                    var stackFrames = stackTrace.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    var topFrames = string.Join(" | ", stackFrames.Take(10).Select(f => f.Trim()));
                    await _errorRepo.AddError(new DbErrorLog
                    {
                        TimestampUTC = DateTime.UtcNow,
                        GameId = game.Id,
                        SeasonStartYear = seasonStartYear,
                        ExceptionType = ex.GetType().FullName ?? ex.GetType().Name,
                        Message = ex.Message,
                        StackTrace = topFrames,
                        Source = "GameCleaner.CleanGamesInSeasons"
                    });
                }
            }

            if (cleanedGames.Count > 0)
            {
                try
                {
                    await _cleanedGameRepo.AddUpdateCleanedGames(cleanedGames);
                    await _cleanedGameRepo.Commit();
                    totalCleaned += cleanedGames.Count;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving final batch for season {Season}.", seasonStartYear);
                }
            }
            _logger.LogInformation("Number of Games Added To Season " + seasonStartYear.ToString() + ": " + totalCleaned.ToString());
        }
    }

    private static IEnumerable<Game> GetGamesToClean(IEnumerable<DbGameCleaned> cleanedGames, IEnumerable<Game> games)
    {
        var gamesToClean = new List<Game>();
        gamesToClean.AddRange(games.GetFutureGames());
        gamesToClean.AddRange(cleanedGames.GetNewGames(games));

        return gamesToClean.GetUniqueGames();
    }
}
