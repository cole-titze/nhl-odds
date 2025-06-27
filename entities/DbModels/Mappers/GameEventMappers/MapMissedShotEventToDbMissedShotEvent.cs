using Entities.DbModels.GamePlayEvents;
using Entities.Models.GamePlayEvents;

namespace Entities.DbModels.Mappers.GameEventMappers
{
    public static class MapMissedShotEventToDbMissedShotEvent
    {
        public static DbMissedShot Map(MissedShot missedShotEvent, int gameId)
        {
            return new DbMissedShot
            {
                id = missedShotEvent.id,
                gameId = gameId,
                typeCode = missedShotEvent.typeCode,
                sortOrder = missedShotEvent.sortOrder,
                situationCode = missedShotEvent.situationCode,
                periodNumber = missedShotEvent.periodNumber,
                periodType = missedShotEvent.periodType,
                eventTypeName = missedShotEvent.eventTypeName,
                homeTeamDefendingSide = missedShotEvent.homeTeamDefendingSide,
                shotType = missedShotEvent.shotType,
                secondsIntoPeriod = missedShotEvent.secondsIntoPeriod,
                secondsLeftInPeriod = missedShotEvent.secondsLeftInPeriod,
                shootingTeamId = missedShotEvent.shootingTeamId,
                shootingPlayerId = missedShotEvent.shootingPlayerId,
                goalieId = missedShotEvent.goalieId,
                xCoordinate = missedShotEvent.xCoordinate,
                yCoordinate = missedShotEvent.yCoordinate,
                zone = missedShotEvent.zone,
                missType = missedShotEvent.missType
            };
        }

        public static IEnumerable<DbMissedShot> MapList(IEnumerable<MissedShot> missedShotEvents, int gameId)
        {
            var dbEvents = new List<DbMissedShot>();
            foreach (var missedShotEvent in missedShotEvents)
            {
                dbEvents.Add(Map(missedShotEvent, gameId));
            }

            return dbEvents;
        }
    }
}