using Entities.Models.GamePlayEvents;
using Entities.Types;
using Entities.Types.Enums;

namespace Entities.ServiceModels.Mappers.GameEventMappers
{
    public static class MapMissedShotEvent
    {
        /// <summary>
        /// Maps a blocked shot event
        /// </summary>
        /// <param name="responseGameEvent">The event response from the NHL api</param>
        /// <returns>The blocked shot event</returns>
        public static MissedShot Map(dynamic responseGameEvent)
        {
            string timeInPeriod = responseGameEvent.timeInPeriod;
            string timeLeftInPeriod = responseGameEvent.timeRemaining;

            return new MissedShot
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
                shotType = ShotTypeParser.ParseFromString((string)responseGameEvent.details.reason),
                shootingPlayerTeamId = (int)responseGameEvent.details.eventOwnerTeamId,
                shootingPlayerId = (int)responseGameEvent.details.shootingPlayerId,
                goalieId = (int)responseGameEvent.details.goalieInNetId,
                zone = ZoneParser.ParseFromString((string)responseGameEvent.details.zoneCode),
                xCoordinate = (int)responseGameEvent.details.xCoord,
                yCoordinate = (int)responseGameEvent.details.yCoord,
                missType = MissedShotTypeParser.ParseFromString((string)responseGameEvent.details.reason)
            };
        }
    }
}