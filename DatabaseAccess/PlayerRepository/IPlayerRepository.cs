using Entities.DbModels;
using Entities.Models;

namespace DatabaseAccess.PlayerRepository
{
    public interface IPlayerRepository
    {
        Task AddUpdateGamePlayerStats(IEnumerable<IGamePlayerStats> gamePlayerStats);
        Task AddUpdatePlayers(IEnumerable<Player> players);
        Task<int> GetPlayerStatsCountBySeason(int seasonStartYear);
    }
}

