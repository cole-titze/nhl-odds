using Entities.DbModels;
using Entities.Models;

namespace DatabaseAccess.PlayerRepository
{
    public interface IPlayerRepository
    {
        Task AddUpdatePlayers(List<DbPlayer> players);
        Task<int> GetPlayerCountBySeason(int seasonStartYear);
        Task<GameRoster> GetGameRoster(Game game);
    }
}

