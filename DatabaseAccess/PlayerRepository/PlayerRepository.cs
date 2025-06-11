using DataAccess.PlayerRepository.Mappers;
using Entities.DbModels;
using Entities.DbModels.Mappers;
using Entities.Models;
using Microsoft.EntityFrameworkCore;

namespace DatabaseAccess.PlayerRepository
{
    public class PlayerRepository : IPlayerRepository
    {
        private readonly NhlDbContext _dbContext;
        public PlayerRepository(NhlDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        /// <summary>
        /// Add Players to database if they don't exist, otherwise update them
        /// </summary>
        /// <param name="playersWithValues">List of players to store</param>
        /// <returns>None</returns>
        public async Task AddUpdatePlayers(IEnumerable<Player> players)
        {
            var dbPlayers = MapPlayerToDbPlayer.Map(players);

            var addList = new List<DbPlayer>();
            var updateList = new List<DbPlayer>();
            foreach (var player in dbPlayers)
            {
                var dbPlayer = await GetDbPlayer(player.id);
                if (dbPlayer == null)
                {
                    addList.Add(player);
                }
                else
                {
                    dbPlayer.Clone(player);
                    updateList.Add(dbPlayer);
                }
            }

            await _dbContext.Player.AddRangeAsync(addList);
            _dbContext.Player.UpdateRange(updateList);
        }
        /// <summary>
        /// Add Players stats per game to database if they don't exist, otherwise update them
        /// </summary>
        /// <param name="playersWithValues">List of players to store</param>
        /// <returns>None</returns>
        public async Task AddUpdateGameRosterStats(IEnumerable<GameRosterStats> seasonGameRosterStats)
        {
            var dbGamePlayerStats = MapGamePlayerStatsToDbGamePlayerStats.Map(seasonGameRosterStats);

            var addList = new List<IDbGamePlayerStats>();
            var updateList = new List<IDbGamePlayerStats>();
            foreach (var playerStats in dbGamePlayerStats)
            {
                var dbPlayerStats = await GetDbGamePlayerStats(playerStats);
                if (dbPlayerStats == null)
                {
                    addList.Add(playerStats);
                }
                else
                {
                    dbPlayerStats.Clone(playerStats);
                    updateList.Add(dbPlayerStats);
                }
            }

            await _dbContext.GameSkaterStats.AddRangeAsync(addList.OfType<DbGameSkaterStats>());
            _dbContext.GameSkaterStats.UpdateRange(updateList.OfType<DbGameSkaterStats>());
            await _dbContext.GameGoalieStats.AddRangeAsync(addList.OfType<DbGameGoalieStats>());
            _dbContext.GameGoalieStats.UpdateRange(updateList.OfType<DbGameGoalieStats>());
        }
        /// <summary>
        /// Gets a player based on the id
        /// </summary>
        /// <param name="playerId">Id of the player to get</param>
        /// <returns>Desired player</returns>
        private async Task<DbPlayer?> GetDbPlayer(int playerId)
        {
            var dbPlayer = await _dbContext.Player.FirstOrDefaultAsync(x => x.id == playerId);
            if (dbPlayer == null)
                return null;

            return dbPlayer;
        }
        /// <summary>
        /// Gets a player's draft details based on the id
        /// </summary>
        /// <param name="playerId">Id of the player to get</param>
        /// <returns>Desired player</returns>
        private async Task<DbPlayerDraftDetails?> GetDbPlayerDraftDetails(int playerId)
        {
            var dbPlayerDraftDetails = await _dbContext.PlayerDraftDetails.FirstOrDefaultAsync(x => x.playerId == playerId);
            if (dbPlayerDraftDetails == null)
                return null;

            return dbPlayerDraftDetails;
        }
        /// <summary>
        /// Gets a stats for a game
        /// </summary>
        /// <param name="gamePlayerStats">The game player stats</param>
        /// <returns>Desired player</returns>
        private async Task<IDbGamePlayerStats?> GetDbGamePlayerStats(IDbGamePlayerStats gamePlayerStats)
        {
            var dbGoalieStats = await _dbContext.GameGoalieStats.FirstOrDefaultAsync(x => x.gameId == gamePlayerStats.gameId && x.playerId == gamePlayerStats.gameId);
            var dbSkaterStats = await _dbContext.GameSkaterStats.FirstOrDefaultAsync(x => x.gameId == gamePlayerStats.gameId && x.playerId == gamePlayerStats.gameId);

            if (dbGoalieStats != null)
                return dbGoalieStats;

            if (dbSkaterStats != null)
                return dbSkaterStats;

            return null;
        }
        /// <summary>
        /// Gets the number of players in the database for a given season.
        /// </summary>
        /// <param name="seasonStartYear">Year to get players for</param>
        /// <returns>Number of players found</returns>
        public async Task<int> GetPlayerStatsCountBySeason(int seasonStartYear)
        {
            var skaterCountTask = _dbContext.GameSkaterStats
                .Include(x => x.game)
                .Where(y => y.game != null && y.game.seasonStartYear == seasonStartYear)
                .CountAsync();
            var goalieCountTask = _dbContext.GameGoalieStats
                .Include(x => x.game)
                .Where(y => y.game != null && y.game.seasonStartYear == seasonStartYear)
                .CountAsync();
                
            await Task.WhenAll(skaterCountTask, goalieCountTask);

            return skaterCountTask.Result + goalieCountTask.Result;
        }

        /// <summary>
        /// Adds or updates player draft details in the database.
        /// </summary>
        /// <param name="players">The players to add draft details for</param>
        /// <returns>None</returns>
        public async Task AddUpdatePlayerDraftDetails(IEnumerable<Player> players)
        {
            var dbPlayersDraftDetails = MapPlayerToDbPlayerDraftDetails.MapList(players);

            var addList = new List<DbPlayerDraftDetails>();
            var updateList = new List<DbPlayerDraftDetails>();
            foreach (var playerDraftDetails in dbPlayersDraftDetails)
            {
                var dbPlayerDraftDetails = await GetDbPlayerDraftDetails(playerDraftDetails.playerId);
                if (dbPlayerDraftDetails == null)
                {
                    addList.Add(playerDraftDetails);
                }
                else
                {
                    dbPlayerDraftDetails.Clone(playerDraftDetails);
                    updateList.Add(dbPlayerDraftDetails);
                }
            }

            await _dbContext.PlayerDraftDetails.AddRangeAsync(addList);
            _dbContext.PlayerDraftDetails.UpdateRange(updateList);
        }
    }
}
