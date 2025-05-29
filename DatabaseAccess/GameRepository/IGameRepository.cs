using Entities.DbModels;

namespace DatabaseAccess.GameRepository
{
    public interface IGameRepository
    {
        Task AddSeasonGameCounts(IDictionary<int, int> seasonGameCountCache);
        Task AddUpdateGames(IEnumerable<DbGameRaw> seasonGames);
        Task Commit();
        Task<DbGameRaw> GetGame(int gameId);
        Task<int> GetGameCountInSeason(int seasonStartYear);
        Task<IEnumerable<DbGameRaw>> GetSeasonGames(int seasonStartYear);
    }
}

