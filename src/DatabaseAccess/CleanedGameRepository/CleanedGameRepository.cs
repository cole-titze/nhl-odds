using Entities.DbModels;
using Microsoft.EntityFrameworkCore;

namespace DatabaseAccess.CleanedGameRepository;

public class CleanedGameRepository : ICleanedGameRepository
{
    private List<DbGameCleaned> _cachedSeasonsGames = new();
    private int? _cachedSeasonYear;
    private readonly NhlDbContext _dbContext;

    public CleanedGameRepository(NhlDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddUpdateCleanedGames(IEnumerable<DbGameCleaned> cleanedGames)
    {
        var addList = new List<DbGameCleaned>();
        foreach (var game in cleanedGames)
        {
            var dbGame = _cachedSeasonsGames.FirstOrDefault(x => x.GameId == game.GameId);
            game.Game = null;
            if (dbGame == null)
            {
                addList.Add(game);
            }
            else if (!dbGame.IsEquivalentTo(game))
            {
                dbGame.Clone(game);
                _dbContext.GameCleaned.Update(dbGame);
            }
        }
        await _dbContext.GameCleaned.AddRangeAsync(addList);
    }

    private async Task CacheSeasonOfCleanedGames(int seasonStartYear)
    {
        if (_cachedSeasonYear != seasonStartYear)
        {
            _cachedSeasonsGames = await _dbContext.GameCleaned
                .AsNoTracking()
                .Where(x => x.GameId / 1000000 == seasonStartYear)
                .ToListAsync();
            _cachedSeasonYear = seasonStartYear;
        }
    }

    public void ClearTracking()
    {
        _dbContext.ChangeTracker.Clear();
    }

    public async Task<IEnumerable<DbGameCleaned>> GetSeasonOfCleanedGames(int seasonStartYear)
    {
        await CacheSeasonOfCleanedGames(seasonStartYear);
        return _cachedSeasonsGames;
    }

    public async Task Commit()
    {
        await _dbContext.SaveChangesAsync();
    }
}