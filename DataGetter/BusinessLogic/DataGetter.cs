using DatabaseAccess.GameRepository;
using DatabaseAccess.PlayerRepository;
using Entities.Models;
using Entities.Types;
using Microsoft.Extensions.Logging;
using Services.NhlData;

namespace DataGetter.BusinessLogic
{
    public class NhlDataManager
    {
        private readonly IGameRepository _gameRepo;
        private readonly IPlayerRepository _playerRepo;
        private readonly NhlDataGetter _nhlDataGetter;
        private readonly ILogger<NhlDataManager> _logger;
        public NhlDataManager(IGameRepository gameRepository, IPlayerRepository playerRepository, NhlDataGetter nhlDataGetter, ILoggerFactory loggerFactory)
        {
            _gameRepo = gameRepository;
            _playerRepo = playerRepository;
            _nhlDataGetter = nhlDataGetter;
            _logger = loggerFactory.CreateLogger<NhlDataManager>();
        }
        /// <summary>
        /// Gets all nhl games within the season range. If the game is already in the database, it is skipped.
        /// </summary>
        /// <param name="seasonYearRange">The years to get data for</param>
        /// <returns>None</returns>
        public async Task GetData(YearRange seasonYearRange)
        {
            int totalGamesAdded = 0;
            for (int seasonStartYear = seasonYearRange.StartYear; seasonStartYear <= seasonYearRange.EndYear; seasonStartYear++)
            {
                // Determines if data is already found and season can be skipped
                var isCurrentYear = seasonStartYear == seasonYearRange.EndYear;
                var hasAllSeasonGames = await HasAllSeasonGames(seasonStartYear);
                if (hasAllSeasonGames && !isCurrentYear)
                {
                    _logger.LogInformation("All game data for season " + seasonStartYear.ToString() + " already exists. Skipping...");
                    continue;
                }

                var seasonGames = await GetAndSaveNhlGameData(seasonStartYear);

                totalGamesAdded += seasonGames.Count();
                _logger.LogInformation("Number of Games Added To Season " + seasonStartYear.ToString() + ": " + seasonGames.Count().ToString());
            }
            var seasonGameCountCache = _nhlDataGetter.ScheduleDataGetter.GetSeasonGameCounts();
            await _gameRepo.AddSeasonGameCounts(seasonGameCountCache);
            await _gameRepo.Commit();
            _logger.LogInformation("Number of Total Games Added: " + totalGamesAdded.ToString());
        }

        /// <summary>
        /// Gets all game and player data for a given season and stores it to the database
        /// </summary>
        /// <param name="seasonStartYear">Season to get data for</param>
        private async Task<IEnumerable<Game>> GetAndSaveNhlGameData(int seasonStartYear)
        {
            // Gets basic game information
            var seasonGameCount = await _nhlDataGetter.ScheduleDataGetter.GetGameCountInSeason(seasonStartYear);
            var seasonGames = await GetSeasonGames(seasonStartYear, seasonGameCount);
            await _gameRepo.AddUpdateGames(seasonGames);

            // Updates tv broadcasters for the games
            await _gameRepo.AddUpdateTvBroadcasters(seasonGames);
            await _gameRepo.AddUpdateGameTvBroadcasters(seasonGames);

            // Update game events and save to the db
            await _gameRepo.AddUpdateGameEvents(seasonGames);

            // Gets player stats for the game
            var gameRosterStats = await BuildGameRosterStats(seasonGames);
            await _playerRepo.AddUpdateGameRosterStats(gameRosterStats);

            // Gets player data for the players who have game stats
            var players = await GetPlayers(gameRosterStats);
            await _playerRepo.AddUpdatePlayers(players);
            await _playerRepo.AddUpdatePlayerDraftDetails(players);

            // Add Officials
            await _gameRepo.AddUpdateGameOfficials(seasonGames);

            // Save all data to the database
            await _gameRepo.Commit();
                
            return seasonGames;
        }

        /// <summary>
        /// Gets if all of a seasons games are already found
        /// </summary>
        /// <param name="seasonStartYear">Season to check</param>
        /// <returns>True if all games exist, otherwise False</returns>
        private async Task<bool> HasAllSeasonGames(int seasonStartYear)
        {
            var gameCount = await _gameRepo.GetGameCountInSeason(seasonStartYear);
            var seasonGameCount = await _nhlDataGetter.ScheduleDataGetter.GetGameCountInSeason(seasonStartYear);
            return gameCount == seasonGameCount;
        }

        /// <summary>
        /// Gets a seasons worth of games. Only returns games that have not already been found.
        /// </summary>
        /// <param name="seasonStartYear">year of games to get</param>
        /// <param name="gameCount">Number of games to get</param>
        /// <returns>List of games from the start year</returns>
        private async Task<IEnumerable<Game>> GetSeasonGames(int seasonStartYear, int gameCount)
        {
            gameCount = 1; // For testing

            var seasonGames = new List<Game>();
            Game? game;
            // game ids start at 1
            for (int count = 1; count <= gameCount; count++)
            {
                var gameId = NhlDataGetter.GetGameId(seasonStartYear, count);
                var existingGame = await _gameRepo.GetGame(gameId);
                if (existingGame != null && existingGame.hasBeenPlayed)
                    continue;

                game = await _nhlDataGetter.GameDataGetter.GetGame(gameId);
                if (game != null)
                    seasonGames.Add(game);
            }

            return seasonGames;
        }
        /// Gets a seasons worth of player stats per game. Only returns games that have not already been found.
        /// </summary>
        /// <param name="seasonGames">Games to get player stats for</param>
        /// <returns>List of player game stats from the start year</returns>
        private async Task<IEnumerable<GameRosterStats>> BuildGameRosterStats(IEnumerable<Game> seasonGames)
        {
            var seasonPlayerGameStats = new List<GameRosterStats>();
            GameRosterStats? gameRosterStats;

            foreach (var game in seasonGames)
            {
                gameRosterStats = await _nhlDataGetter.PlayerDataGetter.BuildGameRosterStats(game);
                if (gameRosterStats != null)
                {
                    seasonPlayerGameStats.Add(gameRosterStats);
                    game.rosterStats = gameRosterStats;
                }
            }

            return seasonPlayerGameStats;
        }

        /// Gets a seasons worth of players. Only returns players that are active
        /// </summary>
        /// <param name="seasonStartYear">year of games to get</param>
        /// <returns>List of games from the start year</returns>
        private async Task<IEnumerable<Player>> GetPlayers(IEnumerable<GameRosterStats> seasonGameRosterStats)
        {
            var uniquePlayerIds = new HashSet<int>();
            foreach (var gameRosterStats in seasonGameRosterStats)
            {
                foreach (var playerStats in gameRosterStats.AllPlayers)
                {
                    uniquePlayerIds.Add(playerStats.playerId);
                }
            }

            var players = new List<Player>();
            foreach (var playerId in uniquePlayerIds)
            {
                var player = await _nhlDataGetter.PlayerDataGetter.GetPlayer(playerId);
                if (player != null)
                    players.Add(player);
            }

            return players;
        }
    }
}
