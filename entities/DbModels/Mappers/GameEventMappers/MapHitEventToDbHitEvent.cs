using Entities.DbModels.GamePlayEvents;
using Entities.Models.GamePlayEvents;

namespace Entities.DbModels.Mappers.GameEventMappers
{
    public static class MapHitEventToDbHitEvent
    {
        public static DbHit Map(Hit hitEvent, int gameId)
        {
            return new DbHit
            {
                id = hitEvent.id,
                gameId = gameId,
                typeCode = hitEvent.typeCode,
                sortOrder = hitEvent.sortOrder,
                situationCode = hitEvent.situationCode,
                periodNumber = hitEvent.periodNumber,
                periodType = hitEvent.periodType,
                eventTypeName = hitEvent.eventTypeName,
                homeTeamDefendingSide = hitEvent.homeTeamDefendingSide,
                secondsIntoPeriod = hitEvent.secondsIntoPeriod,
                secondsLeftInPeriod = hitEvent.secondsLeftInPeriod,
                hittingPlayerTeamId = hitEvent.hittingPlayerTeamId,
                hittingPlayerId = hitEvent.hittingPlayerId,
                hitteePlayerId = hitEvent.hitteePlayerId,
                xCoordinate = hitEvent.xCoordinate,
                yCoordinate = hitEvent.yCoordinate,
                zone = hitEvent.zone
            };
        }

        public static IEnumerable<DbHit> MapList(IEnumerable<Hit> hitEvents, int gameId)
        {
            var dbEvents = new List<DbHit>();
            foreach (var hitEvent in hitEvents)
            {
                dbEvents.Add(Map(hitEvent, gameId));
            }

            return dbEvents;
        }
    }
}