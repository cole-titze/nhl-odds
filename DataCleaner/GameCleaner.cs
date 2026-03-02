using Entities.Types;
using DatabaseAccess.GameSeasonRepository;
using DatabaseAccess.CleanedGameRepository;
using Entities.DbModels;
using DataCleaner.Mappers;
using Entities.Models;
using Microsoft.Extensions.Logging;

namespace DataCleaner;

public class GameCleaner
{
    private readonly IGameSeasonRepository _gameRepo;
    private readonly ICleanedGameRepository _cleanedGameRepo;
    private readonly ILogger<GameCleaner> _logger;

    public GameCleaner(IGameSeasonRepository gameRepository, ICleanedGameRepository cleanedGameRepository, ILoggerFactory loggerFactory)
    {
        _gameRepo = gameRepository;
        _cleanedGameRepo = cleanedGameRepository;
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

            var cleanedGames = new List<DbGameCleaned>();
            foreach (var game in gamesToClean)
            {
                cleanedGames.Add(MapGameToDbGameCleaned.Map(game, gameMap));
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
