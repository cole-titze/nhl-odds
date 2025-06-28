using Entities.Models.GamePlayEvents;
using Entities.Types;
using Entities.Types.Enums;

namespace Entities.ServiceModels.Mappers.GameEventMappers
{
    public static class MapStoppageEvent
    {
        /// <summary>
        /// Maps a stoppage event from the response to a Stoppage object.
        /// </summary>
        /// <param name="responseGameEvent">The event response from the NHL api</param>
        /// <returns>The stoppage event</returns>
        public static Stoppage Map(dynamic responseGameEvent)
        {
            string timeInPeriod = responseGameEvent.timeInPeriod;
            string timeLeftInPeriod = responseGameEvent.timeRemaining;

            return new Stoppage
            {
                id = (int)responseGameEvent.eventId,
                typeCode = (int)responseGameEvent.typeCode,
                sortOrder = (int)responseGameEvent.sortOrder,
                situationCode = SituationCodeParser.ParseFromString((string)responseGameEvent.situationCode),
                periodNumber = (int)responseGameEvent.periodDescriptor.number,
                periodType = PeriodTypeParser.ParseFromString((string)responseGameEvent.periodDescriptor.periodType),
                eventTypeName = responseGameEvent.typeDescKey,
                homeTeamDefendingSide = HomeTeamDefendingSideParser.ParseFromString((string)responseGameEvent.homeTeamDefendingSide),
                secondsIntoPeriod = timeInPeriod.ParseIceTimeToSeconds(),
                secondsLeftInPeriod = timeLeftInPeriod.ParseIceTimeToSeconds(),
                stoppageType = StoppageTypeParser.ParseFromString((string)responseGameEvent.details.reason),
                stoppageDetails = StoppageDetailsParser.ParseFromString(responseGameEvent.details.secondaryReason as string ?? "")
            };
        }
    }
}