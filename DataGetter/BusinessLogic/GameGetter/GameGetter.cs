﻿using DatabaseAccess.GameRepository;
using Entities.DbModels;
using Entities.Models;
using Entities.Types;
using Microsoft.Extensions.Logging;
using Services.NhlData;

namespace DataGetter.BusinessLogic.GameGetter
{
    public class GameGetter
    {
        private readonly IGameRepository _gameRepo;
        private readonly NhlDataGetter _nhlDataGetter;
        private readonly ILogger<GameGetter> _logger;
        public GameGetter(IGameRepository gameRepository, NhlDataGetter nhlDataGetter, ILoggerFactory loggerFactory)
        {
            _gameRepo = gameRepository;
            _nhlDataGetter = nhlDataGetter;
            _logger = loggerFactory.CreateLogger<GameGetter>();
        }
        /// <summary>
        /// Gets all nhl games within the season range. If the game is already in the database, it is skipped.
        /// </summary>
        public async Task GetGames(YearRange seasonYearRange)
        {
            int numberOfGamesAdded = 0;
            for (int seasonStartYear = seasonYearRange.StartYear; seasonStartYear <= seasonYearRange.EndYear; seasonStartYear++)
            {
                if (await SeasonGamesExist(seasonStartYear) && seasonStartYear != seasonYearRange.EndYear)
                {
                    _logger.LogInformation("All game data for season " + seasonStartYear.ToString() + " already exists. Skipping...");
                    continue;
                }

                var seasonGameCount = await _nhlDataGetter.ScheduleDataGetter.GetGameCountInSeason(seasonStartYear);
                var seasonGames = await GetSeasonGames(seasonStartYear, seasonGameCount);
                await _gameRepo.AddUpdateGames(seasonGames);
                var seasonRosters = await GetGameRosters(seasonGames);
                await _gameRepo.AddUpdateRosters(seasonRosters);
                await _gameRepo.Commit();

                numberOfGamesAdded += seasonGames.Count();

                _logger.LogInformation("Number of Games Added To Season " + seasonStartYear.ToString() + ": " + seasonGames.Count().ToString());
            }
            var seasonGameCountCache = _nhlDataGetter.ScheduleDataGetter.GetSeasonGameCounts();
            await _gameRepo.AddSeasonGameCounts(seasonGameCountCache);
            await _gameRepo.Commit();
            _logger.LogInformation("Number of Total Games Added: " + numberOfGamesAdded.ToString());
        }
        /// <summary>
        /// Retrieves rosters for all games
        /// </summary>
        /// <param name="seasonGames">Season games</param>
        /// <returns>List of player rosters</returns>
        private async Task<Dictionary<int, Roster>> GetGameRosters(List<DbGame> seasonGames)
        {
            var rosters = new Dictionary<int, Roster>();
            Roster players;
            foreach (var game in seasonGames)
            {
                rosters.Add(game.id, new Roster());

                players = await _nhlDataGetter.PlayerDataGetter.GetGameRoster(game);

                rosters[game.id] = players;
            }
            return rosters;
        }

        /// <summary>
        /// Gets if all of a seasons games are already found
        /// </summary>
        /// <param name="seasonStartYear">Season to check</param>
        /// <returns>True if all games exist, otherwise False</returns>
        private async Task<bool> SeasonGamesExist(int seasonStartYear)
        {
            var gameCount = await _gameRepo.GetGameCountInSeason(seasonStartYear);
            var seasonGameCount = await _nhlDataGetter.ScheduleDataGetter.GetGameCountInSeason(seasonStartYear);
            return gameCount == seasonGameCount;
        }

        /// <summary>
        /// Gets a seasons worth of games. Only returns games that have not already been found.
        /// </summary>
        /// <param name="seasonstartYear">year of games to get</param>
        /// <param name="gameCount">Number of games to get</param>
        /// <returns>List of games from the start year</returns>
        private async Task<List<DbGame>> GetSeasonGames(int seasonStartYear, int gameCount)
        {
            await _gameRepo.CacheSeasonOfGames(seasonStartYear);

            var seasonGames = new List<DbGame>();
            DbGame game;
            // game ids start at 1
            for (int count = 1; count <= gameCount; count++)
            {
                var gameId = NhlDataGetter.GetGameId(seasonStartYear, count);
                game = _gameRepo.GetGame(gameId);
                if (game.IsValid() && game.hasBeenPlayed)
                    continue;

                game = await _nhlDataGetter.GameDataGetter.GetGame(gameId);
                if (game.IsValid())
                    seasonGames.Add(game);
            }

            return seasonGames;
        }
    }
}
