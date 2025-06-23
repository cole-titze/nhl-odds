using Entities.DbModels.GamePlayEvents;
using Entities.Models.GamePlayEvents;

namespace Entities.DbModels.Mappers.GameEventMappers
{
    public static class MapGoalEventToDbGoalEvent
    {
        public static DbGoal Map(Goal goalEvent, int gameId)
        {
            return new DbGoal
            {
                id = goalEvent.id,
                gameId = gameId,
                typeCode = goalEvent.typeCode,
                sortOrder = goalEvent.sortOrder,
                situationCode = goalEvent.situationCode,
                periodNumber = goalEvent.periodNumber,
                periodType = goalEvent.periodType,
                eventTypeName = goalEvent.eventTypeName,
                homeTeamDefendingSide = goalEvent.homeTeamDefendingSide,
                secondsIntoPeriod = goalEvent.secondsIntoPeriod,
                secondsLeftInPeriod = goalEvent.secondsLeftInPeriod,
                xCoordinate = goalEvent.xCoordinate,
                yCoordinate = goalEvent.yCoordinate,
                zone = goalEvent.zone,
                shotType = goalEvent.shotType,
                scoringPlayerTeamId = goalEvent.scoringPlayerTeamId,
                assistOnePlayerId = goalEvent.assistOnePlayerId,
                assistTwoPlayerId = goalEvent.assistTwoPlayerId,
                scoringPlayerId = goalEvent.scoringPlayerId,
                goalieId = goalEvent.goalieId,
                highlightClipSharingUrl = goalEvent.highlightClipSharingUrl,
                highlightClipId = goalEvent.highlightClipId,
                discreetClipId = goalEvent.discreetClipId,
                pptReplayUrl = goalEvent.pptReplayUrl
            };
        }

        public static IEnumerable<DbGoal> MapList(IEnumerable<Goal> goalEvents, int gameId)
        {
            var dbEvents = new List<DbGoal>();
            foreach (var goalEvent in goalEvents)
            {
                dbEvents.Add(Map(goalEvent, gameId));
            }

            return dbEvents;
        }
    }
}