using Entities.DbModels.GamePlayEvents;
using Entities.Models.GamePlayEvents;

namespace Entities.Mappers.GameEventMappers;

public static class MapPeriodStartEventToDbPeriodStartEvent
{
    public static DbPeriodStart Map(PeriodStart periodStartEvent, int gameId)
    {
        return new DbPeriodStart
        {
            Id = periodStartEvent.Id,
            GameId = gameId,
            TypeCode = periodStartEvent.TypeCode,
            SortOrder = periodStartEvent.SortOrder,
            SituationCode = periodStartEvent.SituationCode,
            PeriodNumber = periodStartEvent.PeriodNumber,
            PeriodType = periodStartEvent.PeriodType,
            EventTypeName = periodStartEvent.EventTypeName,
            HomeTeamDefendingSide = periodStartEvent.HomeTeamDefendingSide,
            SecondsIntoPeriod = periodStartEvent.SecondsIntoPeriod,
            SecondsLeftInPeriod = periodStartEvent.SecondsLeftInPeriod
        };
    }

    public static IEnumerable<DbPeriodStart> MapList(IEnumerable<PeriodStart> periodStartEvents, int gameId)
    {
        var dbEvents = new List<DbPeriodStart>();
        foreach (var periodStartEvent in periodStartEvents)
        {
            dbEvents.Add(Map(periodStartEvent, gameId));
        }

        return dbEvents;
    }
}