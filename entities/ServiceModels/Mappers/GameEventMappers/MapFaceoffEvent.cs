using Entities.Models.GamePlayEvents;
using Entities.Types;
using Entities.Types.Enums;

namespace Entities.ServiceModels.Mappers.GameEventMappers
{
    public static class MapFaceoffEvent
    {
        /// <summary>
        ///   Maps a faceoff event from the response to a Faceoff object.
        /// </summary>
        /// <param name="responseGameEvent">The event response from the NHL api</param>
        /// <returns>The mapped faceoff event</returns>
        public static Faceoff Map(dynamic responseGameEvent)
        {
            string timeInPeriod = responseGameEvent.timeInPeriod;
            string timeLeftInPeriod = responseGameEvent.timeRemaining;

            return new Faceoff
            {
                id = (int)responseGameEvent.eventId,
                typeCode = (int)responseGameEvent.typeCode,
                sortOrder = (int)responseGameEvent.sortOrder,
                situationCode = int.Parse((string)responseGameEvent.situationCode ?? "1551"),
                periodNumber = (int)responseGameEvent.periodDescriptor.number,
                periodType = PeriodTypeParser.ParseFromString((string)responseGameEvent.periodDescriptor.periodType),
                eventTypeName = (string)responseGameEvent.typeDescKey,
                homeTeamDefendingSide = HomeTeamDefendingSideParser.ParseFromString((string?)responseGameEvent.homeTeamDefendingSide),
                secondsIntoPeriod = timeInPeriod.ParseIceTimeToSeconds(),
                secondsLeftInPeriod = timeLeftInPeriod.ParseIceTimeToSeconds(),
                winningTeamId = responseGameEvent.details.eventOwnerTeamId,
                winningPlayerId = responseGameEvent.details.winningPlayerId,
                losingPlayerId = responseGameEvent.details.losingPlayerId,
                xCoordinate = responseGameEvent.details.xCoord,
                yCoordinate = responseGameEvent.details.yCoord,
                zone = ZoneParser.ParseFromString((string)responseGameEvent.details.zoneCode),
            };
        }
    }
}