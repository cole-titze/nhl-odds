using Entities.DbModels.GamePlayEvents;
using Entities.Models.GamePlayEvents;

namespace Entities.DbModels.Mappers.GameEventMappers
{
    public static class MapDelayedPenaltyEventToDbDelayedPenaltyEvent
    {
        public static DbDelayedPenalty Map(DelayedPenalty delayedPenaltyEvent, int gameId)
        {
            return new DbDelayedPenalty
            {
                id = delayedPenaltyEvent.id,
                gameId = gameId,
                typeCode = delayedPenaltyEvent.typeCode,
                sortOrder = delayedPenaltyEvent.sortOrder,
                situationCode = delayedPenaltyEvent.situationCode,
                periodNumber = delayedPenaltyEvent.periodNumber,
                periodType = delayedPenaltyEvent.periodType,
                eventTypeName = delayedPenaltyEvent.eventTypeName,
                homeTeamDefendingSide = delayedPenaltyEvent.homeTeamDefendingSide,
                secondsIntoPeriod = delayedPenaltyEvent.secondsIntoPeriod,
                secondsLeftInPeriod = delayedPenaltyEvent.secondsLeftInPeriod,
                penaltyTeamId = delayedPenaltyEvent.penaltyTeamId
            };
        }

        public static IEnumerable<DbDelayedPenalty> MapList(IEnumerable<DelayedPenalty> delayedPenaltyEvents, int gameId)
        {
            var dbEvents = new List<DbDelayedPenalty>();
            foreach (var delayedPenaltyEvent in delayedPenaltyEvents)
            {
                dbEvents.Add(Map(delayedPenaltyEvent, gameId));
            }

            return dbEvents;
        }
    }
}