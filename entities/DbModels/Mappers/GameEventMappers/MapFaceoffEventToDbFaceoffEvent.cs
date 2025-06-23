using Entities.DbModels.GamePlayEvents;
using Entities.Models.GamePlayEvents;

namespace Entities.DbModels.Mappers.GameEventMappers
{
    public static class MapFaceoffEventToDbFaceoffEvent
    {
        public static DbFaceoff Map(Faceoff faceoffEvent, int gameId)
        {
            return new DbFaceoff
            {
                id = faceoffEvent.id,
                gameId = gameId,
                typeCode = faceoffEvent.typeCode,
                sortOrder = faceoffEvent.sortOrder,
                situationCode = faceoffEvent.situationCode,
                periodNumber = faceoffEvent.periodNumber,
                periodType = faceoffEvent.periodType,
                eventTypeName = faceoffEvent.eventTypeName,
                homeTeamDefendingSide = faceoffEvent.homeTeamDefendingSide,
                secondsIntoPeriod = faceoffEvent.secondsIntoPeriod,
                secondsLeftInPeriod = faceoffEvent.secondsLeftInPeriod,
                winningTeamId = faceoffEvent.winningTeamId,
                winningPlayerId = faceoffEvent.winningPlayerId,
                losingPlayerId = faceoffEvent.losingPlayerId,
                xCoordinate = faceoffEvent.xCoordinate,
                yCoordinate = faceoffEvent.yCoordinate,
                zone = faceoffEvent.zone
            };
        }

        public static IEnumerable<DbFaceoff> MapList(IEnumerable<Faceoff> faceoffEvents, int gameId)
        {
            var dbEvents = new List<DbFaceoff>();
            foreach (var faceoffEvent in faceoffEvents)
            {
                dbEvents.Add(Map(faceoffEvent, gameId));
            }

            return dbEvents;
        }
    }
}