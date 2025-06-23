using Entities.DbModels.GamePlayEvents;
using Entities.Models.GamePlayEvents;

namespace Entities.DbModels.Mappers.GameEventMappers
{
    public static class MapPenaltyEventToDbPenaltyEvent
    {
        public static DbPenalty Map(Penalty penaltyEvent, int gameId)
        {
            return new DbPenalty
            {
                id = penaltyEvent.id,
                gameId = gameId,
                typeCode = penaltyEvent.typeCode,
                sortOrder = penaltyEvent.sortOrder,
                situationCode = penaltyEvent.situationCode,
                periodNumber = penaltyEvent.periodNumber,
                periodType = penaltyEvent.periodType,
                eventTypeName = penaltyEvent.eventTypeName,
                homeTeamDefendingSide = penaltyEvent.homeTeamDefendingSide,
                secondsIntoPeriod = penaltyEvent.secondsIntoPeriod,
                secondsLeftInPeriod = penaltyEvent.secondsLeftInPeriod,
                committedByPlayerTeamId = penaltyEvent.committedByPlayerTeamId,
                drawnByPlayerId = penaltyEvent.drawnByPlayerId,
                committedByPlayerId = penaltyEvent.committedByPlayerId,
                xCoordinate = penaltyEvent.xCoordinate,
                yCoordinate = penaltyEvent.yCoordinate,
                zone = penaltyEvent.zone,
                duration = penaltyEvent.duration,
                penaltyType = penaltyEvent.penaltyType,
                penaltySeverity = penaltyEvent.penaltySeverity
            };
        }

        public static IEnumerable<DbPenalty> MapList(IEnumerable<Penalty> penaltyEvents, int gameId)
        {
            var dbEvents = new List<DbPenalty>();
            foreach (var penaltyEvent in penaltyEvents)
            {
                dbEvents.Add(Map(penaltyEvent, gameId));
            }

            return dbEvents;
        }
    }
}