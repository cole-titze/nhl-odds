using Entities.DbModels;

namespace DatabaseAccess.PlayerRepository
{
    public interface IPlayerRepository
    {
        Task AddUpdatePlayers(List<DbPlayer> players);
        Task<int> GetPlayerCountBySeason(int seasonStartYear);
    }
}

