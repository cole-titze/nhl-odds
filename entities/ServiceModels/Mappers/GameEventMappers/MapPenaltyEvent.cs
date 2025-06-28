using Entities.Models.GamePlayEvents;
using Entities.Types;
using Entities.Types.Enums;

namespace Entities.ServiceModels.Mappers.GameEventMappers
{
    public static class MapPenaltyEvent
    {
        /// <summary>
        /// Maps a penalty event
        /// </summary>
        /// <param name="responseGameEvent">The event response from the NHL api</param>
        /// <returns>The penalty event</returns>
        public static Penalty Map(dynamic responseGameEvent)
        {
            string timeInPeriod = responseGameEvent.timeInPeriod;
            string timeLeftInPeriod = responseGameEvent.timeRemaining;

            return new Penalty
            {
                id = (int)responseGameEvent.eventId,
                typeCode = (int)responseGameEvent.typeCode,
                sortOrder = (int)responseGameEvent.sortOrder,
                situationCode = SituationCodeParser.ParseFromString((string)responseGameEvent.situationCode),
                periodNumber = (int)responseGameEvent.periodDescriptor.number,
                periodType = PeriodTypeParser.ParseFromString((string)responseGameEvent.periodDescriptor.periodType),
                eventTypeName = (string)responseGameEvent.typeDescKey,
                homeTeamDefendingSide = HomeTeamDefendingSideParser.ParseFromString((string?)responseGameEvent.homeTeamDefendingSide),
                secondsIntoPeriod = timeInPeriod.ParseIceTimeToSeconds(),
                secondsLeftInPeriod = timeLeftInPeriod.ParseIceTimeToSeconds(),
                penaltyType = PenaltyTypeParser.ParseFromString((string)responseGameEvent.details.descKey),
                penaltySeverity = PenaltySeverityParser.ParseFromString((string)responseGameEvent.details.typeCode),
                committedByPlayerTeamId = (int)responseGameEvent.details.eventOwnerTeamId,
                drawnByPlayerId = (int?)responseGameEvent.details.drawnByPlayerId,
                servedByPlayerId = (int?)responseGameEvent.details.servedByPlayerId,
                committedByPlayerId = (int?)responseGameEvent.details.committedByPlayerId,
                zone = ZoneParser.ParseFromString((string)responseGameEvent.details.zoneCode),
                xCoordinate = (int?)responseGameEvent.details.xCoord,
                yCoordinate = (int?)responseGameEvent.details.yCoord,
                duration = (int)responseGameEvent.details.duration,
            };
        }
    }
}