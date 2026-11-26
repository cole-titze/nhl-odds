using Entities.DbModels.GamePlayEvents;
using Entities.Models.GamePlayEvents;

namespace Entities.Mappers.GameEventMappers;

public static class MapDbTakeawayEventToTakeawayEvent
{
    public static Takeaway Map(DbTakeaway db)
    {
        return new Takeaway
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
            TakeawayPlayerTeamId = db.TakeawayPlayerTeamId,
            TakeawayPlayerId = db.TakeawayPlayerId,
            XCoordinate = db.XCoordinate,
            YCoordinate = db.YCoordinate,
            Zone = db.Zone
        };
    }

    public static IEnumerable<Takeaway> MapList(IEnumerable<DbTakeaway> dbEvents)
    {
        var events = new List<Takeaway>();
        foreach (var db in dbEvents)
        {
            events.Add(Map(db));
        }
        return events;
    }
}
