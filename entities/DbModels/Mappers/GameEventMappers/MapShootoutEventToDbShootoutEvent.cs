using Entities.DbModels.GamePlayEvents;
using Entities.Models.GamePlayEvents;

namespace Entities.DbModels.Mappers.GameEventMappers
{
    public static class MapShootoutCompleteEventToDbShootoutCompleteEvent
    {
        public static DbShootoutComplete Map(ShootoutComplete periodEndEvent, int gameId)
        {
            return new DbShootoutComplete
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

        public static IEnumerable<DbShootoutComplete> MapList(IEnumerable<ShootoutComplete> periodEndEvents, int gameId)
        {
            var dbEvents = new List<DbShootoutComplete>();
            foreach (var periodEndEvent in periodEndEvents)
            {
                dbEvents.Add(Map(periodEndEvent, gameId));
            }

            return dbEvents;
        }
    }
}