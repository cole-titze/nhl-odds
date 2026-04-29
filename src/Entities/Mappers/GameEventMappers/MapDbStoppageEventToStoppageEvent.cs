using Entities.DbModels.GamePlayEvents;
using Entities.Models.GamePlayEvents;

namespace Entities.Mappers.GameEventMappers;

public static class MapDbStoppageEventToStoppageEvent
{
    public static Stoppage Map(DbStoppage db)
    {
        return new Stoppage
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
            StoppageType = db.StoppageType,
            StoppageDetails = db.StoppageDetails
        };
    }

    public static IEnumerable<Stoppage> MapList(IEnumerable<DbStoppage> dbEvents)
    {
        var events = new List<Stoppage>();
        foreach (var db in dbEvents)
        {
            events.Add(Map(db));
        }
        return events;
    }
}