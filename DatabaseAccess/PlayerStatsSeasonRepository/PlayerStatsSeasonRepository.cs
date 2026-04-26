using Entities.DbModels;
using Microsoft.EntityFrameworkCore;

namespace DatabaseAccess.PlayerStatsSeasonRepository;

public class PlayerStatsSeasonRepository : IPlayerStatsSeasonRepository
{
    private readonly NhlDbContext _dbContext;

    public PlayerStatsSeasonRepository(NhlDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<DbGameSkaterStats>> GetSeasonSkaterStats(int seasonStartYear)
    {
        return await _dbContext.GameSkaterStats
            .Include(s => s.Game)
            .Where(s => s.Game != null && s.Game.SeasonStartYear == seasonStartYear)
            .ToListAsync();
    }

    public async Task<IEnumerable<DbGameGoalieStats>> GetSeasonGoalieStats(int seasonStartYear)
    {
        return await _dbContext.GameGoalieStats
            .Include(s => s.Game)
            .Where(s => s.Game != null && s.Game.SeasonStartYear == seasonStartYear)
            .ToListAsync();
    }
}