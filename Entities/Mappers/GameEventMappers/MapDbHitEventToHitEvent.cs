using Entities.DbModels.GamePlayEvents;
using Entities.Models.GamePlayEvents;

namespace Entities.Mappers.GameEventMappers;

public static class MapDbHitEventToHitEvent
{
    public static Hit Map(DbHit db)
    {
        return new Hit
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
            HittingPlayerTeamId = db.HittingPlayerTeamId,
            HittingPlayerId = db.HittingPlayerId,
            HitteePlayerId = db.HitteePlayerId,
            XCoordinate = db.XCoordinate,
            YCoordinate = db.YCoordinate,
            Zone = db.Zone
        };
    }

    public static IEnumerable<Hit> MapList(IEnumerable<DbHit> dbEvents)
    {
        var events = new List<Hit>();
        foreach (var db in dbEvents)
        {
            events.Add(Map(db));
        }
        return events;
    }
}
