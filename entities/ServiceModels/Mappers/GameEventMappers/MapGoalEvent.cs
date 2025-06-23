using Entities.Models.GamePlayEvents;
using Entities.Types;
using Entities.Types.Enums;

namespace Entities.ServiceModels.Mappers.GameEventMappers
{
    public static class MapGoalEvent
    {
        /// <summary>
        /// Maps a goal event
        /// </summary>
        /// <param name="responseGameEvent">The event response from the NHL api</param>
        /// <returns>The goal event</returns>
        public static Goal Map(dynamic responseGameEvent)
        {
            string timeInPeriod = responseGameEvent.timeInPeriod;
            string timeLeftInPeriod = responseGameEvent.timeRemaining;

            return new Goal
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
                shotType = ShotTypeParser.ParseFromString((string)responseGameEvent.details.shotType),
                scoringPlayerTeamId = (int)responseGameEvent.details.eventOwnerTeamId,
                scoringPlayerId = (int)responseGameEvent.details.scoringPlayerId,
                goalieId = (int)responseGameEvent.details.goalieInNetId,
                assistOnePlayerId = (int)responseGameEvent.details.assist1PlayerId,
                assistTwoPlayerId = (int)responseGameEvent.details.assist2PlayerId,
                zone = ZoneParser.ParseFromString((string)responseGameEvent.details.zoneCode),
                xCoordinate = (int)responseGameEvent.details.xCoord,
                yCoordinate = (int)responseGameEvent.details.yCoord,
                highlightClipSharingUrl = (string)responseGameEvent.details.highlightClipSharingUrl,
                highlightClipId = (int)responseGameEvent.details.highlightClip,
                discreetClipId = (int)responseGameEvent.details.discreteClip,
                pptReplayUrl = (string)responseGameEvent.pptReplayUrl,
            };
        }
    }
}