using Entities.DbModels;
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
        public async Task AddUpdatePlayers(List<DbPlayer> playersWithValues)
        {
            var addList = new List<DbPlayer>();
            var updateList = new List<DbPlayer>();
            foreach (var player in playersWithValues)
            {
                player.value = BuildPlayerValue(player);
                if (player.name == null || player.name == "")
                    continue;

                var dbPlayer = _dbContext.PlayerValue.FirstOrDefault(p => p.id == player.id && p.seasonStartYear == player.seasonStartYear);
                if (dbPlayer == null)
                    addList.Add(player);
                else
                {
                    dbPlayer.value = player.value;
                    dbPlayer.position = player.position;
                    updateList.Add(dbPlayer);
                }
            }
            _dbContext.PlayerValue.AddRange(addList);
            _dbContext.PlayerValue.UpdateRange(updateList);

            await _dbContext.SaveChangesAsync();
        }
        /// <summary>
        /// Checks for a player value. If the player has a value of 0 attempts to use the previous years value, otherwise keeps 0.
        /// </summary>
        /// <param name="player">The player to fill the value for</param>
        /// <returns>The player with a filled value</returns>
        private double BuildPlayerValue(DbPlayer player)
        {
            if (player.value == 0)
            {
                var playerLastYear = _dbContext.PlayerValue.FirstOrDefault(p => p.id == player.id && p.seasonStartYear == player.seasonStartYear - 1);
                if (playerLastYear != null)
                    player.value = playerLastYear.value;
            }

            return player.value;
        }

        /// <summary>
        /// Gets the number of players in the database for a given season.
        /// </summary>
        /// <param name="seasonStartYear">Year to get players for</param>
        /// <returns>Number of players found</returns>
        public async Task<int> GetPlayerCountBySeason(int seasonStartYear)
        {
            return await _dbContext.PlayerValue.Where(s => s.seasonStartYear == seasonStartYear).CountAsync();
        }
                /// <summary>
        /// Gets the game roster for a given game
        /// </summary>
        /// <param name="game">Game to get roster for</param>
        /// <returns>Game roster</returns>
        public async Task<GameRoster> GetGameRoster(Game game)
        {
            DbPlayer? playerValue;
            var roster = new GameRoster();
            var players = await _dbContext.GamePlayer.Where(x => x.gameId == game.id).ToListAsync();
            foreach(var player in players)
            {
                playerValue = _dbContext.PlayerValue.Where(x => x.seasonStartYear == game.seasonStartYear && x.id == player.playerId).FirstOrDefault();
                if (playerValue == null)
                    continue;

                if(player.teamId == game.homeTeamId)
                {
                    if (playerValue.position == "D")
                        roster.HomeDefensePlayers.Add(playerValue);
                    else if (playerValue.position == "G")
                        roster.HomeGoalies.Add(playerValue);
                    else
                        roster.HomeOffensePlayers.Add(playerValue);
                }
                else
                {
                    if (playerValue.position == "D")
                        roster.AwayDefensePlayers.Add(playerValue);
                    else if (playerValue.position == "G")
                        roster.AwayGoalies.Add(playerValue);
                    else
                        roster.AwayOffensePlayers.Add(playerValue);
                }
            }
            return roster;
        }
    }
}
