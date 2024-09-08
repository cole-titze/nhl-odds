﻿using Entities.DbModels;
using Entities.Models;
using Microsoft.EntityFrameworkCore;

namespace DatabaseAccess.GameRepository
{
    public class GameRepository : IGameRepository
    {
        private List<DbGame> _cachedSeasonsGames = new List<DbGame>();
        private Dictionary<int, int> _seasonGameCountCache = new Dictionary<int, int>();
        private readonly NhlDbContext _dbContext;
        public GameRepository(NhlDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        /// <summary>
        /// Gets total games for given season in database
        /// </summary>
        /// <param name="seasonStartYear">season start year</param>
        /// <returns>number of games in the season</returns>
        public async Task<int> GetGameCountInSeason(int seasonStartYear)
        {
            return await _dbContext.Game.Where(s => s.seasonStartYear == seasonStartYear).CountAsync();
        }
        /// <summary>
        /// Updates games to the database and adds them if they don't exist
        /// </summary>
        /// <param name="games">List of games to add or update</param>
        /// <returns>None</returns>
        public async Task AddUpdateGames(List<DbGame> games)
        {
            var addList = new List<DbGame>();
            var updateList = new List<DbGame>();
            foreach (var game in games)
            {
                var dbGame = _cachedSeasonsGames.FirstOrDefault(x => x.id == game.id);
                if (dbGame == null)
                    addList.Add(game);
                else
                {
                    dbGame.Clone(game);
                    updateList.Add(dbGame);
                }
            }
            await _dbContext.Game.AddRangeAsync(addList);
            _dbContext.Game.UpdateRange(updateList);
        }
        /// <summary>
        /// Gets a seasons worth of games and stores them in the cache variable
        /// </summary>
        /// <param name="seasonStartYear">Season start year</param>
        /// <returns>None</returns>
        public async Task CacheSeasonOfGames(int seasonStartYear)
        {
            _cachedSeasonsGames = await _dbContext.Game.Where(s => s.seasonStartYear == seasonStartYear)
                                        .Include(x => x.awayTeam)
                                        .Include(x => x.homeTeam)
                                        .ToListAsync();
        }
        /// <summary>
        /// Gets if a game exists in cache
        /// </summary>
        /// <param name="gameId">Game to check</param>
        /// <returns>True if the game exists, otherwise false</returns>
        public bool GameExistsInCache(int gameId)
        {
            var game = _cachedSeasonsGames.FirstOrDefault(i => i.id == gameId);
            if (game == null)
                return false;
            return true;
        }
        /// <summary>
        /// Adds player rosters to the database if they don't exist. Removes players that are no longer on the roster for the game
        /// </summary>
        /// <param name="rosters">List of players mapped to games</param>
        /// <returns>None</returns>
        public async Task AddUpdateRosters(Dictionary<int, Roster> rosters)
        {
            List<DbGamePlayer> oldRosters = new List<DbGamePlayer>();
            foreach (var key in rosters.Keys)
            {
                oldRosters.AddRange(_dbContext.GamePlayer.Where(x => x.gameId == key));
            }

            var addList = new List<DbGamePlayer>();
            foreach (var roster in rosters)
            {
                foreach (var player in roster.Value.homeTeam)
                {
                    BuildDbRoster(addList, oldRosters, player);
                }
                foreach (var player in roster.Value.awayTeam)
                {
                    BuildDbRoster(addList, oldRosters, player);
                }
            }

            await _dbContext.GamePlayer.AddRangeAsync(addList);
            _dbContext.GamePlayer.RemoveRange(oldRosters);
        }

        /// <summary>
        /// Determines what players should be added and removed
        /// </summary>
        /// <param name="addList">List of players to add (reference)</param>
        /// <param name="oldRosters">List of players to remove (reference)</param>
        /// <param name="player">The player to check</param>
        private void BuildDbRoster(List<DbGamePlayer> addList, List<DbGamePlayer> oldRosters, DbGamePlayer player)
        {
            DbGamePlayer? dbPlayer;

            dbPlayer = oldRosters.FirstOrDefault(x => x.gameId == player.gameId && x.playerId == player.playerId);
            if (dbPlayer == null)
                addList.Add(player);
            else
                oldRosters.Remove(player);
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
        public DbGame GetGame(int gameId)
        {
            var game = _cachedSeasonsGames.FirstOrDefault(x => x.id == gameId);
            if (game == null)
                return new DbGame();

            return game;
        }
        /// <summary>
        /// Gets the Season game counts. Caches the first call from the database.
        /// </summary>
        /// <returns>Dictionary of season key and game count value</returns>
        public async Task<Dictionary<int, int>> GetSeasonGameCounts()
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
        public async Task AddSeasonGameCounts(Dictionary<int, int> seasonGameCountCache)
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

