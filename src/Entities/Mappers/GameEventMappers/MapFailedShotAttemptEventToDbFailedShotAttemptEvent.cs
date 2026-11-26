using Entities.DbModels.GamePlayEvents;
using Entities.Models.GamePlayEvents;

namespace Entities.Mappers.GameEventMappers;

public static class MapFailedShotAttemptEventToDbFailedShotAttemptEvent
{
    public static DbFailedShotAttempt Map(FailedShotAttempt failedShotAttemptEvent, int gameId)
    {
        return new DbFailedShotAttempt
        {
            Id = failedShotAttemptEvent.Id,
            GameId = gameId,
            TypeCode = failedShotAttemptEvent.TypeCode,
            SortOrder = failedShotAttemptEvent.SortOrder,
            SituationCode = failedShotAttemptEvent.SituationCode,
            PeriodNumber = failedShotAttemptEvent.PeriodNumber,
            PeriodType = failedShotAttemptEvent.PeriodType,
            EventTypeName = failedShotAttemptEvent.EventTypeName,
            HomeTeamDefendingSide = failedShotAttemptEvent.HomeTeamDefendingSide,
            ShotType = failedShotAttemptEvent.ShotType,
            SecondsIntoPeriod = failedShotAttemptEvent.SecondsIntoPeriod,
            SecondsLeftInPeriod = failedShotAttemptEvent.SecondsLeftInPeriod,
            ShootingTeamId = failedShotAttemptEvent.ShootingTeamId,
            ShootingPlayerId = failedShotAttemptEvent.ShootingPlayerId,
            GoalieId = failedShotAttemptEvent.GoalieId,
            XCoordinate = failedShotAttemptEvent.XCoordinate,
            YCoordinate = failedShotAttemptEvent.YCoordinate,
            Zone = failedShotAttemptEvent.Zone,
        };
    }

    public static IEnumerable<DbFailedShotAttempt> MapList(IEnumerable<FailedShotAttempt> failedShotAttemptEvents, int gameId)
    {
        var dbEvents = new List<DbFailedShotAttempt>();
        foreach (var failedShotAttemptEvent in failedShotAttemptEvents)
        {
            dbEvents.Add(Map(failedShotAttemptEvent, gameId));
        }

        return dbEvents;
    }
}
