using Entities.DbModels;

namespace DatabaseAccess.CleanedGameRepository;

public interface ICleanedGameRepository
{
    Task<IEnumerable<DbGameCleaned>> GetSeasonOfCleanedGames(int seasonStartYear);
    Task AddUpdateCleanedGames(IEnumerable<DbGameCleaned> cleanedGames);
    Task Commit();
    void ClearTracking();
}