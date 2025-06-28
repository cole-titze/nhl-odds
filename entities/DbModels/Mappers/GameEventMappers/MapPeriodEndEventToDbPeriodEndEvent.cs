using Entities.DbModels.GamePlayEvents;
using Entities.Models.GamePlayEvents;

namespace Entities.DbModels.Mappers.GameEventMappers;

public static class MapPeriodEndEventToDbPeriodEndEvent
{
    public static DbPeriodEnd Map(PeriodStart periodEndEvent, int gameId)
    {
        return new DbPeriodEnd
        {
            Id = periodEndEvent.Id,
            GameId = gameId,
            TypeCode = periodEndEvent.TypeCode,
            SortOrder = periodEndEvent.SortOrder,
            SituationCode = periodEndEvent.SituationCode,
            PeriodNumber = periodEndEvent.PeriodNumber,
            PeriodType = periodEndEvent.PeriodType,
            EventTypeName = periodEndEvent.EventTypeName,
            HomeTeamDefendingSide = periodEndEvent.HomeTeamDefendingSide,
            SecondsIntoPeriod = periodEndEvent.SecondsIntoPeriod,
            SecondsLeftInPeriod = periodEndEvent.SecondsLeftInPeriod
        };
    }

    public static IEnumerable<DbPeriodEnd> MapList(IEnumerable<PeriodStart> periodEndEvents, int gameId)
    {
        var dbEvents = new List<DbPeriodEnd>();
        foreach (var periodEndEvent in periodEndEvents)
        {
            dbEvents.Add(Map(periodEndEvent, gameId));
        }

        return dbEvents;
    }
}