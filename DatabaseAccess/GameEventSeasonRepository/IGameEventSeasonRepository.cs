using Entities.DbModels.GamePlayEvents;

namespace DatabaseAccess.GameEventSeasonRepository;

public interface IGameEventSeasonRepository
{
    Task<IEnumerable<DbPenalty>> GetSeasonPenalties(int seasonStartYear);
    Task<IEnumerable<DbGoal>> GetSeasonGoals(int seasonStartYear);
    Task<IEnumerable<DbFaceoff>> GetSeasonFaceoffs(int seasonStartYear);
    Task<IEnumerable<DbMissedShot>> GetSeasonMissedShots(int seasonStartYear);
}