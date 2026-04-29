using Entities.DbModels.GamePlayEvents;
using Entities.Models.GamePlayEvents;

namespace Entities.Mappers.GameEventMappers;

public static class MapTakeawayEventToDbTakeawayEvent
{
    public static DbTakeaway Map(Takeaway takeawayEvent, int gameId)
    {
        return new DbTakeaway
        {
            Id = takeawayEvent.Id,
            GameId = gameId,
            TypeCode = takeawayEvent.TypeCode,
            SortOrder = takeawayEvent.SortOrder,
            SituationCode = takeawayEvent.SituationCode,
            PeriodNumber = takeawayEvent.PeriodNumber,
            PeriodType = takeawayEvent.PeriodType,
            EventTypeName = takeawayEvent.EventTypeName,
            HomeTeamDefendingSide = takeawayEvent.HomeTeamDefendingSide,
            SecondsIntoPeriod = takeawayEvent.SecondsIntoPeriod,
            SecondsLeftInPeriod = takeawayEvent.SecondsLeftInPeriod,
            TakeawayPlayerTeamId = takeawayEvent.TakeawayPlayerTeamId,
            TakeawayPlayerId = takeawayEvent.TakeawayPlayerId,
            XCoordinate = takeawayEvent.XCoordinate,
            YCoordinate = takeawayEvent.YCoordinate,
            Zone = takeawayEvent.Zone
        };
    }

    public static IEnumerable<DbTakeaway> MapList(IEnumerable<Takeaway> takeawayEvents, int gameId)
    {
        var dbEvents = new List<DbTakeaway>();
        foreach (var takeawayEvent in takeawayEvents)
        {
            dbEvents.Add(Map(takeawayEvent, gameId));
        }

        return dbEvents;
    }
}