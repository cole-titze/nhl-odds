using Entities.DbModels;

namespace DatabaseAccess.CleanedGameRepository
{
	public interface ICleanedGameRepository
	{
        public Task<IDictionary<int,IEnumerable<DbCleanedGame>>> GetSeasonOfCleanedGames(int seasonStartYear);
        public Task AddUpdateCleanedGames(IEnumerable<DbCleanedGame> cleanedGames);
        public Task Commit();
    }
}
