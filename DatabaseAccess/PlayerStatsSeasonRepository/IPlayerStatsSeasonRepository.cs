using Entities.DbModels;

namespace DatabaseAccess.PlayerStatsSeasonRepository;

public interface IPlayerStatsSeasonRepository
{
    Task<IEnumerable<DbGameSkaterStats>> GetSeasonSkaterStats(int seasonStartYear);
    Task<IEnumerable<DbGameGoalieStats>> GetSeasonGoalieStats(int seasonStartYear);
}