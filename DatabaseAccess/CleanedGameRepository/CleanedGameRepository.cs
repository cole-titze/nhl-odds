using Entities.DbModels;
using Microsoft.EntityFrameworkCore;

namespace DatabaseAccess.CleanedGameRepository
{
	public class CleanedGameRepository : ICleanedGameRepository
	{
        private IDictionary<int, IEnumerable<DbCleanedGame>> _cachedSeasonsGames = new Dictionary<int, IEnumerable<DbCleanedGame>>();
        private readonly NhlDbContext _dbContext;
        public CleanedGameRepository(NhlDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        /// <summary>
        /// Adds cleaned games if they don't exist. Updates them if they exist
        /// </summary>
        /// <param name="cleanedGames">List of cleaned games to add to database</param>
        /// <returns>None</returns>
        public async Task AddUpdateCleanedGames(IEnumerable<DbCleanedGame> cleanedGames)
        {
            var addList = new List<DbCleanedGame>();
            var updateList = new List<DbCleanedGame>();
            foreach (var game in cleanedGames)
            {
                var dbGame = await GetCleanGame(game.gameId);
                game.game = null;
                if (dbGame == null)
                {
                    addList.Add(game);
                }
                else
                {
                    dbGame.Clone(game);
                    updateList.Add(dbGame);
                }
            }
            await _dbContext.CleanedGame.AddRangeAsync(addList);
            _dbContext.CleanedGame.UpdateRange(updateList);
        }

        /// <summary>
        /// Gets a cleaned game based on the id
        /// </summary>
        /// <param name="gameId">Id of the game to get</param>
        /// <returns>Desired game</returns>
        public async Task<DbCleanedGame> GetCleanGame(int gameId)
        {
            // Get the season start year from the game id
            int seasonStartYear = int.Parse(gameId.ToString().Substring(0, 4));
            var seasonGames = await GetSeasonOfCleanedGames(seasonStartYear);

            var game = seasonGames[seasonStartYear].FirstOrDefault(x => x.gameId == gameId);
            if (game == null)
                return new DbCleanedGame();

            return game;
        }

        /// <summary>
        /// Gets a seasons worth of cleaned games and stores them in the cache variable if it doesn't already exist
        /// </summary>
        /// <param name="seasonStartYear">Season start year</param>
        /// <returns>None</returns>
        private async Task CacheSeasonOfCleanedGames(int seasonStartYear)
        {
            if (_cachedSeasonsGames.ContainsKey(seasonStartYear) && _cachedSeasonsGames[seasonStartYear].Count() > 0)
                return;

            _cachedSeasonsGames.Clear();
            _cachedSeasonsGames[seasonStartYear] = await _dbContext.CleanedGame.Include(x => x.game).Where(x => x.game!.seasonStartYear == seasonStartYear).ToListAsync();
        }
        /// <summary>
        /// Gets a seasons worth of cleaned games
        /// </summary>
        /// <param name="seasonStartYear">Year to get games for</param>
        /// <returns>List of seasons cleaned games</returns>
        public async Task<IDictionary<int,IEnumerable<DbCleanedGame>>> GetSeasonOfCleanedGames(int seasonStartYear)
        {
            await CacheSeasonOfCleanedGames(seasonStartYear);
            return _cachedSeasonsGames;
        }
        /// <summary>
        /// Saves Database changes
        /// </summary>
        /// <returns>None</returns>
        public async Task Commit()
        {
            await _dbContext.SaveChangesAsync();
        }
    }
}

