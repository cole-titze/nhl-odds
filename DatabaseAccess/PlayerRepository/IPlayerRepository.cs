using Entities.DbModels;
using Entities.Models;

namespace DatabaseAccess.PlayerRepository
{
    public interface IPlayerRepository
    {
        Task AddUpdateGameRosterStats(IEnumerable<GameRosterStats> gameRosterStats);
        Task AddUpdatePlayers(IEnumerable<Player> players);
        Task<int> GetPlayerStatsCountBySeason(int seasonStartYear);
    }
}

