using Entities.Models.GamePlayEvents;
using Entities.Types;
using Entities.Types.Enums;

namespace Entities.ServiceModels.Mappers.GameEventMappers
{
    public static class MapShootoutCompleteEvent
    {
        /// <summary>
        ///  Maps a shootout event from the response to a ShootoutComplete object.
        /// </summary>
        /// <param name="responseGameEvent">The event response from the NHL api</param>
        /// <returns>The shootout event</returns>
        public static ShootoutComplete Map(dynamic responseGameEvent)
        {
            string timeInPeriod = responseGameEvent.timeInPeriod;
            string timeLeftInPeriod = responseGameEvent.timeRemaining;

            return new ShootoutComplete
            {
                id = (int)responseGameEvent.eventId,
                typeCode = (int)responseGameEvent.typeCode,
                sortOrder = (int)responseGameEvent.sortOrder,
                situationCode = int.TryParse((string?)responseGameEvent.situationCode, out var situation) ? situation : -1,
                periodNumber = (int)responseGameEvent.periodDescriptor.number,
                periodType = PeriodTypeParser.ParseFromString((string)responseGameEvent.periodDescriptor.periodType),
                eventTypeName = (string)responseGameEvent.typeDescKey,
                homeTeamDefendingSide = HomeTeamDefendingSideParser.ParseFromString((string?)responseGameEvent.homeTeamDefendingSide),
                secondsIntoPeriod = timeInPeriod.ParseIceTimeToSeconds(),
                secondsLeftInPeriod = timeLeftInPeriod.ParseIceTimeToSeconds(),
            };
        }
    }
}