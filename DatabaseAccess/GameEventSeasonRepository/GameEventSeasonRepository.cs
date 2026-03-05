using Entities.DbModels.GamePlayEvents;
using Microsoft.EntityFrameworkCore;

namespace DatabaseAccess.GameEventSeasonRepository;

public class GameEventSeasonRepository : IGameEventSeasonRepository
{
    private readonly NhlDbContext _dbContext;

    public GameEventSeasonRepository(NhlDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<DbPenalty>> GetSeasonPenalties(int seasonStartYear)
    {
        int minGameId = seasonStartYear * 1_000_000;
        int maxGameId = (seasonStartYear + 1) * 1_000_000;
        return await _dbContext.GamePenaltyEvent
            .Where(p => p.GameId >= minGameId && p.GameId < maxGameId)
            .ToListAsync();
    }

    public async Task<IEnumerable<DbGoal>> GetSeasonGoals(int seasonStartYear)
    {
        int minGameId = seasonStartYear * 1_000_000;
        int maxGameId = (seasonStartYear + 1) * 1_000_000;
        return await _dbContext.GameGoalEvent
            .Where(g => g.GameId >= minGameId && g.GameId < maxGameId)
            .ToListAsync();
    }

    public async Task<IEnumerable<DbFaceoff>> GetSeasonFaceoffs(int seasonStartYear)
    {
        int minGameId = seasonStartYear * 1_000_000;
        int maxGameId = (seasonStartYear + 1) * 1_000_000;
        return await _dbContext.GameFaceoffEvent
            .Where(f => f.GameId >= minGameId && f.GameId < maxGameId)
            .ToListAsync();
    }

    public async Task<IEnumerable<DbMissedShot>> GetSeasonMissedShots(int seasonStartYear)
    {
        int minGameId = seasonStartYear * 1_000_000;
        int maxGameId = (seasonStartYear + 1) * 1_000_000;
        return await _dbContext.GameMissedShotEvent
            .Where(m => m.GameId >= minGameId && m.GameId < maxGameId)
            .ToListAsync();
    }
}
