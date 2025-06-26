using Entities.Models.GamePlayEvents;
using Entities.Types;
using Entities.Types.Enums;

namespace Entities.ServiceModels.Mappers.GameEventMappers
{
    public static class MapBlockedShotEvent
    {
        /// <summary>
        /// Maps a blocked shot event
        /// </summary>
        /// <param name="responseGameEvent">The event response from the NHL api</param>
        /// <returns>The blocked shot event</returns>
        public static BlockedShot Map(dynamic responseGameEvent)
        {
            string timeInPeriod = responseGameEvent.timeInPeriod;
            string timeLeftInPeriod = responseGameEvent.timeRemaining;

            return new BlockedShot
            {
                id = (int)responseGameEvent.eventId,
                typeCode = (int)responseGameEvent.typeCode,
                sortOrder = (int)responseGameEvent.sortOrder,
                situationCode = int.Parse((string)responseGameEvent.situationCode),
                periodNumber = (int)responseGameEvent.periodDescriptor.number,
                periodType = PeriodTypeParser.ParseFromString((string)responseGameEvent.periodDescriptor.periodType),
                eventTypeName = responseGameEvent.typeDescKey,
                homeTeamDefendingSide = HomeTeamDefendingSideParser.ParseFromString((string)responseGameEvent.homeTeamDefendingSide),
                secondsIntoPeriod = timeInPeriod.ParseIceTimeToSeconds(),
                secondsLeftInPeriod = timeLeftInPeriod.ParseIceTimeToSeconds(),
                blockType = BlockTypeParser.ParseFromString((string?)responseGameEvent.details.reason),
                blockingPlayerTeamId = (int)responseGameEvent.details.eventOwnerTeamId,
                blockingPlayerId = (int)responseGameEvent.details.blockingPlayerId,
                shooterPlayerId = (int)responseGameEvent.details.shootingPlayerId,
                zone = ZoneParser.ParseFromString((string)responseGameEvent.details.zoneCode),
                xCoordinate = (int)responseGameEvent.details.xCoord,
                yCoordinate = (int)responseGameEvent.details.yCoord,
            };
        }
    }
}