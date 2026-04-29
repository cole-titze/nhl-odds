using Entities.DbModels.GamePlayEvents;
using Entities.Models.GamePlayEvents;

namespace Entities.Mappers.GameEventMappers;

public static class MapShootoutCompleteEventToDbShootoutCompleteEvent
{
    public static DbShootoutComplete Map(ShootoutComplete periodEndEvent, int gameId)
    {
        return new DbShootoutComplete
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

    public static IEnumerable<DbShootoutComplete> MapList(IEnumerable<ShootoutComplete> periodEndEvents, int gameId)
    {
        var dbEvents = new List<DbShootoutComplete>();
        foreach (var periodEndEvent in periodEndEvents)
        {
            dbEvents.Add(Map(periodEndEvent, gameId));
        }

        return dbEvents;
    }
}