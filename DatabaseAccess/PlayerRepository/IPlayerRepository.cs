using Entities.DbModels;
using Entities.Models;

namespace DatabaseAccess.PlayerRepository
{
    public interface IPlayerRepository
    {
        Task AddUpdateGamePlayerStats(IEnumerable<IDbGamePlayerStats> gamePlayerStats);
        Task AddUpdatePlayers(IEnumerable<DbPlayer> players);
        Task<int> GetPlayerStatsCountBySeason(int seasonStartYear);
    }
}

