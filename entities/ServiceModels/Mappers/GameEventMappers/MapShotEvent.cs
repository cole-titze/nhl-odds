using Entities.Models.GamePlayEvents;
using Entities.Types;
using Entities.Types.Enums;

namespace Entities.ServiceModels.Mappers.GameEventMappers
{
    public static class MapShotEvent
    {
        /// <summary>
        ///  Maps a shot event from the response to a ShotEvent object.
        /// </summary>
        /// <param name="responseGameEvent">The event response from the NHL api</param>
        /// <returns>The shot event</returns>
        public static Shot Map(dynamic responseGameEvent)
        {
            string timeInPeriod = responseGameEvent.timeInPeriod;
            string timeLeftInPeriod = responseGameEvent.timeRemaining;

            return new Shot
            {
                id = (int)responseGameEvent.eventId,
                typeCode = (int)responseGameEvent.typeCode,
                sortOrder = (int)responseGameEvent.sortOrder,
                situationCode = int.Parse((string)responseGameEvent.situationCode),
                periodNumber = (int)responseGameEvent.periodDescriptor.number,
                periodType = PeriodTypeParser.ParseFromString((string)responseGameEvent.periodDescriptor.periodType),
                eventTypeName = (string)responseGameEvent.typeDescKey,
                homeTeamDefendingSide = HomeTeamDefendingSideParser.ParseFromString((string)responseGameEvent.homeTeamDefendingSide),
                secondsIntoPeriod = timeInPeriod.ParseIceTimeToSeconds(),
                secondsLeftInPeriod = timeLeftInPeriod.ParseIceTimeToSeconds(),
                shotType = ShotTypeParser.ParseFromString((string)responseGameEvent.details.shotType),
                zone = ZoneParser.ParseFromString((string)responseGameEvent.details.zoneCode),
                shooterTeamId = responseGameEvent.details.eventOwnerTeamId,
                shooterPlayerId = responseGameEvent.details.shootingPlayerId,
                goalieId = responseGameEvent.details.goalieInNetId,
                xCoordinate = responseGameEvent.details.xCoord,
                yCoordinate = responseGameEvent.details.yCoord,
            };
        }
    }
}