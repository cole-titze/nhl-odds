using Entities.DbModels.GamePlayEvents;
using Entities.Models.GamePlayEvents;

namespace Entities.DbModels.Mappers.GameEventMappers
{
    public static class MapPeriodEndEventToDbPeriodEndEvent
    {
        public static DbPeriodEnd Map(PeriodStart periodEndEvent, int gameId)
        {
            return new DbPeriodEnd
            {
                id = periodEndEvent.id,
                gameId = gameId,
                typeCode = periodEndEvent.typeCode,
                sortOrder = periodEndEvent.sortOrder,
                situationCode = periodEndEvent.situationCode,
                periodNumber = periodEndEvent.periodNumber,
                periodType = periodEndEvent.periodType,
                eventTypeName = periodEndEvent.eventTypeName,
                homeTeamDefendingSide = periodEndEvent.homeTeamDefendingSide,
                secondsIntoPeriod = periodEndEvent.secondsIntoPeriod,
                secondsLeftInPeriod = periodEndEvent.secondsLeftInPeriod
            };
        }

        public static IEnumerable<DbPeriodEnd> MapList(IEnumerable<PeriodStart> periodEndEvents, int gameId)
        {
            var dbEvents = new List<DbPeriodEnd>();
            foreach (var periodEndEvent in periodEndEvents)
            {
                dbEvents.Add(Map(periodEndEvent, gameId));
            }

            return dbEvents;
        }
    }
}