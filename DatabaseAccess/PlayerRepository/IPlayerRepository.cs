using Entities.DbModels;
using Entities.Models;

namespace DatabaseAccess.PlayerRepository
{
    public interface IPlayerRepository
    {
        Task AddUpdateGameRosterStats(IEnumerable<GameRosterStats> gameRosterStats);
        Task AddUpdatePlayerDraftDetails(IEnumerable<Player> players);
        Task AddUpdatePlayers(IEnumerable<Player> players);
        Task<int> GetPlayerStatsCountBySeason(int seasonStartYear);
    }
}

