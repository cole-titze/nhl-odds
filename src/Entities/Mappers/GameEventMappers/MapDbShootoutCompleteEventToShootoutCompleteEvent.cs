using Entities.DbModels.GamePlayEvents;
using Entities.Models.GamePlayEvents;

namespace Entities.Mappers.GameEventMappers;

public static class MapDbShootoutCompleteEventToShootoutCompleteEvent
{
    public static ShootoutComplete Map(DbShootoutComplete db)
    {
        return new ShootoutComplete
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
            SecondsLeftInPeriod = db.SecondsLeftInPeriod
        };
    }

    public static IEnumerable<ShootoutComplete> MapList(IEnumerable<DbShootoutComplete> dbEvents)
    {
        var events = new List<ShootoutComplete>();
        foreach (var db in dbEvents)
        {
            events.Add(Map(db));
        }
        return events;
    }
}