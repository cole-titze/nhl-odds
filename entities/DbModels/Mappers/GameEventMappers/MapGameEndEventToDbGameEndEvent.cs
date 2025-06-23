using Entities.DbModels.GamePlayEvents;
using Entities.Models.GamePlayEvents;

namespace Entities.DbModels.Mappers.GameEventMappers
{
    public static class MapGameEndEventToDbGameEndEvent
    {
        public static DbGameEnd Map(GameEnd gameEndEvent, int gameId)
        {
            return new DbGameEnd
            {
                id = gameEndEvent.id,
                gameId = gameId,
                typeCode = gameEndEvent.typeCode,
                sortOrder = gameEndEvent.sortOrder,
                situationCode = gameEndEvent.situationCode,
                periodNumber = gameEndEvent.periodNumber,
                periodType = gameEndEvent.periodType,
                eventTypeName = gameEndEvent.eventTypeName,
                homeTeamDefendingSide = gameEndEvent.homeTeamDefendingSide,
                secondsIntoPeriod = gameEndEvent.secondsIntoPeriod,
                secondsLeftInPeriod = gameEndEvent.secondsLeftInPeriod
            };
        }

        public static IEnumerable<DbGameEnd> MapList(IEnumerable<GameEnd> gameEndEvents, int gameId)
        {
            var dbEvents = new List<DbGameEnd>();
            foreach (var gameEndEvent in gameEndEvents)
            {
                dbEvents.Add(Map(gameEndEvent, gameId));
            }

            return dbEvents;
        }
    }
}