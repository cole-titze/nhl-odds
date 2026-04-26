using Entities.DbModels.GamePlayEvents;
using Entities.Models.GamePlayEvents;

namespace Entities.Mappers.GameEventMappers;

public static class MapDbShotEventToShotEvent
{
    public static Shot Map(DbShot db)
    {
        return new Shot
        {
            Id = db.Id,
            TypeCode = db.TypeCode,
            SortOrder = db.SortOrder,
            SituationCode = db.SituationCode,
            PeriodNumber = db.PeriodNumber,
            PeriodType = db.PeriodType,
            EventTypeName = db.EventTypeName,
            HomeTeamDefendingSide = db.HomeTeamDefendingSide,
            SecondsIntoPeriod = db.SecondsIntoPeriod,
            SecondsLeftInPeriod = db.SecondsLeftInPeriod,
            ShooterTeamId = db.ShootingTeamId,
            ShooterPlayerId = db.ShootingPlayerId,
            GoalieId = db.GoalieId,
            XCoordinate = db.XCoordinate,
            YCoordinate = db.YCoordinate,
            Zone = db.Zone,
            ShotType = db.ShotType
        };
    }

    public static IEnumerable<Shot> MapList(IEnumerable<DbShot> dbEvents)
    {
        var events = new List<Shot>();
        foreach (var db in dbEvents)
        {
            events.Add(Map(db));
        }
        return events;
    }
}