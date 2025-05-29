using DatabaseAccess.GameRepository;
using DatabaseAccess.PlayerRepository;
using Entities.DbModels;
using Entities.Types;
using Microsoft.Extensions.Logging;
using Services.NhlData;

namespace DataGetter.BusinessLogic.GameGetter
{
    public class GameGetter
    {
        private readonly IGameRepository _gameRepo;
        private readonly IPlayerRepository _playerRepo;
        private readonly NhlDataGetter _nhlDataGetter;
        private readonly ILogger<GameGetter> _logger;
        public GameGetter(IGameRepository gameRepository, IPlayerRepository playerRepository, NhlDataGetter nhlDataGetter, ILoggerFactory loggerFactory)
        {
            _gameRepo = gameRepository;
            _playerRepo = playerRepository;
            _nhlDataGetter = nhlDataGetter;
            _logger = loggerFactory.CreateLogger<GameGetter>();
        }
        /// <summary>
        /// Gets all nhl games within the season range. If the game is already in the database, it is skipped.
        /// </summary>
        public async Task GetData(YearRange seasonYearRange)
        {
            int numberOfGamesAdded = 0;
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

                // Gets basic game information
                var seasonGameCount = await _nhlDataGetter.ScheduleDataGetter.GetGameCountInSeason(seasonStartYear);
                var seasonGames = await GetSeasonGames(seasonStartYear, seasonGameCount);
                await _gameRepo.AddUpdateGames(seasonGames);

                // Gets player stats for the game
                var gamePlayerStats = await GetPlayerGameStats(seasonGames);
                await _playerRepo.AddUpdateGamePlayerStats(gamePlayerStats);

                // Gets players for the players who have game stats
                var players = await GetPlayers(gamePlayerStats);
                await _playerRepo.AddUpdatePlayers(players);

                // Save all data to the database
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
        private async Task<List<DbGameRaw>> GetSeasonGames(int seasonStartYear, int gameCount)
        {
            var seasonGames = new List<DbGameRaw>();
            DbGameRaw game;
            // game ids start at 1
            for (int count = 1; count <= gameCount; count++)
            {
                var gameId = NhlDataGetter.GetGameId(seasonStartYear, count);
                game = await _gameRepo.GetGame(gameId);
                if (game.IsValid() && game.hasBeenPlayed)
                    continue;

                game = await _nhlDataGetter.GameDataGetter.GetGame(gameId);
                if (game.IsValid())
                    seasonGames.Add(game);
            }

            return seasonGames;
        }
        /// Gets a seasons worth of player stats per game. Only returns games that have not already been found.
        /// </summary>
        /// <param name="seasonGames">Games to get player stats for</param>
        /// <returns>List of player game stats from the start year</returns>
        private async Task<IEnumerable<IDbGamePlayerStats>> GetPlayerGameStats(IEnumerable<DbGameRaw> seasonGames)
        {
            var seasonPlayerGameStats = new List<IDbGamePlayerStats>();
            IEnumerable<IDbGamePlayerStats> gamePlayerStats;

            foreach (var game in seasonGames)
            {
                gamePlayerStats = await _nhlDataGetter.PlayerDataGetter.GetPlayerGameStats(game);
                foreach (var playerStats in gamePlayerStats)
                {
                    if (playerStats.IsValid())
                        seasonPlayerGameStats.Add(playerStats);
                }
            }

            return seasonPlayerGameStats;
        }
        /// Gets a seasons worth of players. Only returns players that are active
        /// </summary>
        /// <param name="seasonStartYear">year of games to get</param>
        /// <returns>List of games from the start year</returns>
        private async Task<IEnumerable<DbPlayer>> GetPlayers(IEnumerable<IDbGamePlayerStats> gamePlayerStats)
        {
            var uniquePlayerIds = new HashSet<int>();
            foreach (var playerStats in gamePlayerStats)
            {
                uniquePlayerIds.Add(playerStats.playerId);
            }

            var players = new List<DbPlayer>();
            foreach (var playerId in uniquePlayerIds)
            {
                var player = await _nhlDataGetter.PlayerDataGetter.GetPlayer(playerId);
                if (player.IsValid())
                    players.Add(player);
            }

            return players;
        }
    }
}
