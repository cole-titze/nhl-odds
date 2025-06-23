using Entities.DbModels.GamePlayEvents;
using Entities.Models.GamePlayEvents;

namespace Entities.DbModels.Mappers.GameEventMappers
{
    public static class MapTakeawayEventToDbTakeawayEvent
    {
        public static DbTakeaway Map(Takeaway takeawayEvent, int gameId)
        {
            return new DbTakeaway
            {
                id = takeawayEvent.id,
                gameId = gameId,
                typeCode = takeawayEvent.typeCode,
                sortOrder = takeawayEvent.sortOrder,
                situationCode = takeawayEvent.situationCode,
                periodNumber = takeawayEvent.periodNumber,
                periodType = takeawayEvent.periodType,
                eventTypeName = takeawayEvent.eventTypeName,
                homeTeamDefendingSide = takeawayEvent.homeTeamDefendingSide,
                secondsIntoPeriod = takeawayEvent.secondsIntoPeriod,
                secondsLeftInPeriod = takeawayEvent.secondsLeftInPeriod,
                takeawayPlayerTeamId = takeawayEvent.takeawayPlayerTeamId,
                takeawayPlayerId = takeawayEvent.takeawayPlayerId,
                xCoordinate = takeawayEvent.xCoordinate,
                yCoordinate = takeawayEvent.yCoordinate,
                zone = takeawayEvent.zone
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
}