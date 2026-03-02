using Entities.Types;
using DatabaseAccess.GameSeasonRepository;
using DatabaseAccess.CleanedGameRepository;
using DatabaseAccess.ErrorRepository;
using DatabaseAccess.PlayerStatsSeasonRepository;
using Entities.DbModels;
using DataCleaner.Mappers;
using Entities.Models;
using Microsoft.Extensions.Logging;

namespace DataCleaner;

public class GameCleaner
{
    private readonly IGameSeasonRepository _gameRepo;
    private readonly ICleanedGameRepository _cleanedGameRepo;
    private readonly IPlayerStatsSeasonRepository _playerStatsRepo;
    private readonly IErrorRepository _errorRepo;
    private readonly ILogger<GameCleaner> _logger;

    public GameCleaner(IGameSeasonRepository gameRepository, ICleanedGameRepository cleanedGameRepository,
        IPlayerStatsSeasonRepository playerStatsRepository, IErrorRepository errorRepository, ILoggerFactory loggerFactory)
    {
        _gameRepo = gameRepository;
        _cleanedGameRepo = cleanedGameRepository;
        _playerStatsRepo = playerStatsRepository;
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

            var cleanedGames = new List<DbGameCleaned>();
            foreach (var game in gamesToClean)
            {
                try
                {
                    cleanedGames.Add(MapGameToDbGameCleaned.Map(game, gameMap, rosterScorer));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error cleaning game {GameId} in season {Season}. Skipping.", game.Id, seasonStartYear);
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

            await _cleanedGameRepo.AddUpdateCleanedGames(cleanedGames);
            await _cleanedGameRepo.Commit();
            _logger.LogInformation("Number of Games Added To Season " + seasonStartYear.ToString() + ": " + cleanedGames.Count.ToString());
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
