using Entities.DbModels.GamePlayEvents;
using Entities.Models.GamePlayEvents;

namespace Entities.Mappers.GameEventMappers;

public static class MapDbMissedShotEventToMissedShotEvent
{
    public static MissedShot Map(DbMissedShot db)
    {
        return new MissedShot
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
            MissType = db.MissType
        };
    }

    public static IEnumerable<MissedShot> MapList(IEnumerable<DbMissedShot> dbEvents)
    {
        var events = new List<MissedShot>();
        foreach (var db in dbEvents)
        {
            events.Add(Map(db));
        }
        return events;
    }
}
