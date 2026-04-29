using Entities.DbModels.GamePlayEvents;
using Entities.Models.GamePlayEvents;

namespace Entities.Mappers.GameEventMappers;

public static class MapStoppageEventToDbStoppageEvent
{
    public static DbStoppage Map(Stoppage stoppageEvent, int gameId)
    {
        return new DbStoppage
        {
            Id = stoppageEvent.Id,
            GameId = gameId,
            TypeCode = stoppageEvent.TypeCode,
            SortOrder = stoppageEvent.SortOrder,
            SituationCode = stoppageEvent.SituationCode,
            PeriodNumber = stoppageEvent.PeriodNumber,
            PeriodType = stoppageEvent.PeriodType,
            EventTypeName = stoppageEvent.EventTypeName,
            HomeTeamDefendingSide = stoppageEvent.HomeTeamDefendingSide,
            SecondsIntoPeriod = stoppageEvent.SecondsIntoPeriod,
            SecondsLeftInPeriod = stoppageEvent.SecondsLeftInPeriod,
            StoppageType = stoppageEvent.StoppageType,
            StoppageDetails = stoppageEvent.StoppageDetails
        };
    }

    public static IEnumerable<DbStoppage> MapList(IEnumerable<Stoppage> stoppageEvents, int gameId)
    {
        var dbEvents = new List<DbStoppage>();
        foreach (var stoppageEvent in stoppageEvents)
        {
            dbEvents.Add(Map(stoppageEvent, gameId));
        }

        return dbEvents;
    }
}