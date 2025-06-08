using DataAccess.GameRepository.Mappers;
using Entities.DbModels;
using Entities.DbModels.Mappers;
using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DatabaseAccess.GameRepository
{
    public class GameRepository : IGameRepository
    {
        private Dictionary<int, List<DbGameRaw>> _cachedSeasonsGames = new Dictionary<int, List<DbGameRaw>>();
        private Dictionary<int, int> _seasonGameCountCache = new Dictionary<int, int>();
        private readonly NhlDbContext _dbContext;
        private readonly ILogger<GameRepository> _logger;

        public GameRepository(NhlDbContext dbContext, ILoggerFactory loggerFactory)
        {
            _dbContext = dbContext;
            _logger = loggerFactory.CreateLogger<GameRepository>();
        }
        
        /// <summary>
        /// Gets total games for given season in database
        /// </summary>
        /// <param name="seasonStartYear">season start year</param>
        /// <returns>number of games in the season</returns>
        public async Task<int> GetGameCountInSeason(int seasonStartYear)
        {
            return await _dbContext.GameRaw.Where(s => s.seasonStartYear == seasonStartYear).CountAsync();
        }

        /// <summary>
        /// Updates games to the database and adds them if they don't exist
        /// </summary>
        /// <param name="games">List of games to add or update</param>
        /// <returns>None</returns>
        public async Task AddUpdateGames(IEnumerable<Game> games)
        {
            var dbGames = MapGameToDbGame.Map(games);

            var addList = new List<DbGameRaw>();
            var updateList = new List<DbGameRaw>();
            foreach (var game in dbGames)
            {
                var dbGame = await GetDbGame(game.id);
                if (dbGame == null)
                    addList.Add(game);
                else
                {
                    dbGame.Clone(game);
                    updateList.Add(dbGame);
                }
            }
            await _dbContext.GameRaw.AddRangeAsync(addList);
            _dbContext.GameRaw.UpdateRange(updateList);
        }

        /// <summary>
        /// Adds or updates the TV broadcasters for the games.
        /// </summary>
        /// <param name="games">The list of games that contain broadcaster info</param>
        public async Task AddUpdateGameTvBroadcasters(IEnumerable<Game> games)
        {
            var gameBroadcasters = MapGameToDbGameTvBroadcasters.MapList(games);

            var addList = new List<DbGameTvBroadcaster>();
            var updateList = new List<DbGameTvBroadcaster>();
            foreach (var gameBroadcaster in gameBroadcasters)
            {
                var dbTvBroadcaster = await GetGameDbTvBroadcaster(gameBroadcaster.gameId, gameBroadcaster.broadcasterId);
                if (dbTvBroadcaster == null)
                    addList.Add(gameBroadcaster);
                else
                {
                    dbTvBroadcaster.Clone(gameBroadcaster);
                    updateList.Add(dbTvBroadcaster);
                }
            }

            await _dbContext.GameTvBroadcaster.AddRangeAsync(addList);
            _dbContext.GameTvBroadcaster.UpdateRange(updateList);
        }

        /// <summary>
        /// Adds or updates the TV broadcasters for the games.
        /// </summary>
        /// <param name="games">The games to add broadcasters for</param>
        public async Task AddUpdateTvBroadcasters(IEnumerable<Game> games)
        {
            var dbTvBroadcasters = MapGameToDbTvBroadcasters.MapList(games);
            var uniqueTvBroadcasters = dbTvBroadcasters.GroupBy(b => b.id).Select(g => g.First()).ToList();
            
            var addList = new List<DbTvBroadcaster>();
            var updateList = new List<DbTvBroadcaster>();
            foreach (var broadcaster in uniqueTvBroadcasters)
            {
                var dbTvBroadcaster = await GetDbTvBroadcaster(broadcaster.id);
                if (dbTvBroadcaster == null)
                    addList.Add(broadcaster);
                else
                {
                    dbTvBroadcaster.Clone(broadcaster);
                    updateList.Add(dbTvBroadcaster);
                }
            }

            await _dbContext.TvBroadcaster.AddRangeAsync(addList);
            _dbContext.TvBroadcaster.UpdateRange(updateList);
        }

        /// <summary>
        /// Gets a TV broadcaster from the database based on the id
        /// </summary>
        /// <param name="id">The tv broadcaster id</param>
        /// <returns>Tv broadcaster or null if it doesn't exist</returns>
        private async Task<DbTvBroadcaster?> GetDbTvBroadcaster(int id)
        {
            return await _dbContext.TvBroadcaster.FirstOrDefaultAsync(x => x.id == id);
        }

        /// <summary>
        /// Gets a game TV broadcaster from the database based on the game id and tv broadcaster id
        /// </summary>
        /// <param name="gameId">The game Id</param>
        /// <param name="tvBroadcasterId"><The broadcaster id/param>
        /// <returns>The game broadcaster object, or null if it doesn't exist</returns>
        private async Task<DbGameTvBroadcaster?> GetGameDbTvBroadcaster(int gameId, int tvBroadcasterId)
        {
            return await _dbContext.GameTvBroadcaster.FirstOrDefaultAsync(x => x.gameId == gameId && x.broadcasterId == tvBroadcasterId);
        }

        /// <summary>
        /// Gets a seasons worth of games and stores them in the cache variable
        /// </summary>
        /// <param name="seasonStartYear">Season start year</param>
        /// <returns>None</returns>
        private async Task CacheSeasonOfGames(int seasonStartYear)
        {
            if (_cachedSeasonsGames.ContainsKey(seasonStartYear) && _cachedSeasonsGames[seasonStartYear].Count > 0)
                return;

            _cachedSeasonsGames.Clear();
            _cachedSeasonsGames[seasonStartYear] = await _dbContext.GameRaw.Where(s => s.seasonStartYear == seasonStartYear)
                                        .Include(x => x.awayTeam)
                                        .Include(x => x.homeTeam)
                                        .ToListAsync();
        }

        /// <summary>
        /// Gets a seasons worth of games from the database and caches them
        /// </summary>
        /// <param name="seasonStartYear">Season start year</param>
        /// <returns>Seasons games</returns>
        private async Task<IEnumerable<DbGameRaw>> GetSeasonDbGames(int seasonStartYear)
        {
            await CacheSeasonOfGames(seasonStartYear);

            return _cachedSeasonsGames[seasonStartYear];
        }

        /// <summary>
        /// Saves Database changes
        /// </summary>
        /// <returns>None</returns>
        public async Task Commit()
        {
            await _dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Gets a game based on the id
        /// </summary>
        /// <param name="gameId">Id of the game to get</param>
        /// <returns>Desired game</returns>
        public async Task<Game?> GetGame(int gameId)
        {
            // Get the season start year from the game id
            int seasonStartYear = int.Parse(gameId.ToString().Substring(0, 4));
            var seasonGames = await GetSeasonDbGames(seasonStartYear);

            var game = seasonGames.FirstOrDefault(x => x.id == gameId);
            if (game == null)
                return null;

            return MapDbGameToGame.Map(game);
        }

        /// <summary>
        /// Gets a db game based on the id
        /// </summary>
        /// <param name="gameId">Id of the game to get</param>
        /// <returns>Desired game</returns>
        private async Task<DbGameRaw?> GetDbGame(int gameId)
        {
            // Get the season start year from the game id
            int seasonStartYear = int.Parse(gameId.ToString().Substring(0, 4));
            var seasonGames = await GetSeasonDbGames(seasonStartYear);

            var game = seasonGames.FirstOrDefault(x => x.id == gameId);
            if (game == null)
                return null;

            return game;
        }

        /// <summary>
        /// Gets the Season game counts. Caches the first call from the database.
        /// </summary>
        /// <returns>Dictionary of season key and game count value</returns>
        public async Task<IDictionary<int, int>> GetSeasonGameCounts()
        {
            if (_seasonGameCountCache.Keys.Count != 0)
                return _seasonGameCountCache;

            var seasonGameCounts = await _dbContext.SeasonGameCount.ToListAsync();

            foreach (var dbGameCount in seasonGameCounts)
            {
                _seasonGameCountCache.Add(dbGameCount.seasonId, dbGameCount.gameCount);
            }

            return _seasonGameCountCache;
        }

        /// <summary>
        /// Adds the season game counts to the database
        /// </summary>
        /// <param name="seasonGameCountCache">The dictionary of seasonGameCounts to add to the database if they don't exist</param>
        /// <returns></returns>
        public async Task AddSeasonGameCounts(IDictionary<int, int> seasonGameCountCache)
        {
            var seasonGameCounts = new List<DbSeasonGameCount>();
            var dbGameCounts = await _dbContext.SeasonGameCount.ToListAsync();

            foreach (var key in seasonGameCountCache.Keys)
            {
                var dbGameCount = dbGameCounts.FirstOrDefault(x => x.seasonId == key);
                if (dbGameCount == null)
                {
                    seasonGameCounts.Add(new DbSeasonGameCount()
                    {
                        seasonId = key,
                        gameCount = seasonGameCountCache[key],
                    });
                }
            }

            await _dbContext.SeasonGameCount.AddRangeAsync(seasonGameCounts);
        }
    }
}

