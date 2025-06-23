using Entities.DbModels.GamePlayEvents;
using Entities.Models.GamePlayEvents;

namespace Entities.DbModels.Mappers.GameEventMappers
{
    public static class MapBlockedShotEventToDbBlockedShotEvent
    {
        public static DbBlockedShot Map(BlockedShot blockedShotEvent, int gameId)
        {
            return new DbBlockedShot
            {
                id = blockedShotEvent.id,
                gameId = gameId,
                typeCode = blockedShotEvent.typeCode,
                sortOrder = blockedShotEvent.sortOrder,
                situationCode = blockedShotEvent.situationCode,
                periodNumber = blockedShotEvent.periodNumber,
                periodType = blockedShotEvent.periodType,
                eventTypeName = blockedShotEvent.eventTypeName,
                homeTeamDefendingSide = blockedShotEvent.homeTeamDefendingSide,
                secondsIntoPeriod = blockedShotEvent.secondsIntoPeriod,
                secondsLeftInPeriod = blockedShotEvent.secondsLeftInPeriod,
                blockingPlayerTeamId = blockedShotEvent.blockingPlayerTeamId,
                blockingPlayerId = blockedShotEvent.blockingPlayerId,
                shooterPlayerId = blockedShotEvent.shooterPlayerId,
                xCoordinate = blockedShotEvent.xCoordinate,
                yCoordinate = blockedShotEvent.yCoordinate,
                zone = blockedShotEvent.zone,
                blockType = blockedShotEvent.blockType
            };
        }

        public static IEnumerable<DbBlockedShot> MapList(IEnumerable<BlockedShot> blockedShotEvents, int gameId)
        {
            var dbEvents = new List<DbBlockedShot>();
            foreach (var blockedShotEvent in blockedShotEvents)
            {
                dbEvents.Add(Map(blockedShotEvent, gameId));
            }

            return dbEvents;
        }
    }
}