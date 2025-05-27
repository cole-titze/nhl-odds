using Entities.DbModels;
using Entities.Models;

namespace DatabaseAccess.GameRepository
{
    public interface IGameRepository
    {
        Task AddSeasonGameCounts(IDictionary<int, int> seasonGameCountCache);
        Task AddUpdateGames(IEnumerable<DbGame> seasonGames);
        Task AddUpdateRosters(IDictionary<int, Roster> rosters);
        Task CacheSeasonOfGames(int seasonStartYear);
        Task Commit();
        bool GameExistsInCache(int gameId);
        Task<DbGame> GetGame(int gameId);
        Task<int> GetGameCountInSeason(int year);
        Task<IDictionary<int, int>> GetSeasonGameCounts();
        Task<IEnumerable<DbGame>> GetSeasonGames(int seasonStartYear);
        Task<IEnumerable<Game>> GetRichSeasonGames(int seasonStartYear);

    }
}

