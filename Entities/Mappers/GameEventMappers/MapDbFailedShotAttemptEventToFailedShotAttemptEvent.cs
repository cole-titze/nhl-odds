using Entities.DbModels.GamePlayEvents;
using Entities.Models.GamePlayEvents;

namespace Entities.Mappers.GameEventMappers;

public static class MapDbFailedShotAttemptEventToFailedShotAttemptEvent
{
    public static FailedShotAttempt Map(DbFailedShotAttempt db)
    {
        return new FailedShotAttempt
        {
            Id = db.Id,
            TypeCode = db.TypeCode,
            SortOrder = db.SortOrder,
            SituationCode = db.SituationCode,
            PeriodNumber = db.PeriodNumber,
            PeriodType = db.PeriodType,
            EventTypeName = db.EventTypeName,
            HomeTeamDefendingSide = db.HomeTeamDefendingSide,
            ShotType = db.ShotType,
            SecondsIntoPeriod = db.SecondsIntoPeriod,
            SecondsLeftInPeriod = db.SecondsLeftInPeriod,
            ShootingTeamId = db.ShootingTeamId,
            ShootingPlayerId = db.ShootingPlayerId,
            GoalieId = db.GoalieId,
            XCoordinate = db.XCoordinate,
            YCoordinate = db.YCoordinate,
            Zone = db.Zone,
        };
    }

    public static IEnumerable<FailedShotAttempt> MapList(IEnumerable<DbFailedShotAttempt> dbEvents)
    {
        var events = new List<FailedShotAttempt>();
        foreach (var db in dbEvents)
        {
            events.Add(Map(db));
        }
        return events;
    }
}