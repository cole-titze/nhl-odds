using Entities.DbModels.GamePlayEvents;
using Entities.Models.GamePlayEvents;

namespace Entities.DbModels.Mappers.GameEventMappers
{
    public static class MapStoppageEventToDbStoppageEvent
    {
        public static DbStoppage Map(Stoppage stoppageEvent, int gameId)
        {
            return new DbStoppage
            {
                id = stoppageEvent.id,
                gameId = gameId,
                typeCode = stoppageEvent.typeCode,
                sortOrder = stoppageEvent.sortOrder,
                situationCode = stoppageEvent.situationCode,
                periodNumber = stoppageEvent.periodNumber,
                periodType = stoppageEvent.periodType,
                eventTypeName = stoppageEvent.eventTypeName,
                homeTeamDefendingSide = stoppageEvent.homeTeamDefendingSide,
                secondsIntoPeriod = stoppageEvent.secondsIntoPeriod,
                secondsLeftInPeriod = stoppageEvent.secondsLeftInPeriod,
                stoppageType = stoppageEvent.stoppageType,
                stoppageDetails = stoppageEvent.stoppageDetails
            };
        }

        public static IEnumerable<DbStoppage> MapList(IEnumerable<Stoppage> stoppageEvents, int gameId)
        {
            var dbEvents = new List<DbStoppage>();
            foreach (var stoppageEvent in stoppageEvents)
            {
                dbEvents.Add(Map(stoppageEvent, gameId));
            }

            return dbEvents;
        }
    }
}