using Entities.DbModels.GamePlayEvents;
using Entities.Models.GamePlayEvents;

namespace Entities.DbModels.Mappers.GameEventMappers
{
    public static class MapPeriodStartEventToDbPeriodStartEvent
    {
        public static DbPeriodStart Map(PeriodStart periodStartEvent, int gameId)
        {
            return new DbPeriodStart
            {
                id = periodStartEvent.id,
                gameId = gameId,
                typeCode = periodStartEvent.typeCode,
                sortOrder = periodStartEvent.sortOrder,
                situationCode = periodStartEvent.situationCode,
                periodNumber = periodStartEvent.periodNumber,
                periodType = periodStartEvent.periodType,
                eventTypeName = periodStartEvent.eventTypeName,
                homeTeamDefendingSide = periodStartEvent.homeTeamDefendingSide,
                secondsIntoPeriod = periodStartEvent.secondsIntoPeriod,
                secondsLeftInPeriod = periodStartEvent.secondsLeftInPeriod
            };
        }

        public static IEnumerable<DbPeriodStart> MapList(IEnumerable<PeriodStart> periodStartEvents, int gameId)
        {
            var dbEvents = new List<DbPeriodStart>();
            foreach (var periodStartEvent in periodStartEvents)
            {
                dbEvents.Add(Map(periodStartEvent, gameId));
            }

            return dbEvents;
        }
    }
}