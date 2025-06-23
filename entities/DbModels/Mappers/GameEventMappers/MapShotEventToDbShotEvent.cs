using Entities.DbModels.GamePlayEvents;
using Entities.Models.GamePlayEvents;

namespace Entities.DbModels.Mappers.GameEventMappers
{
    public static class MapShotEventToDbShotEvent
    {
        public static DbShot Map(Shot shotEvent, int gameId)
        {
            return new DbShot
            {
                id = shotEvent.id,
                gameId = gameId,
                typeCode = shotEvent.typeCode,
                sortOrder = shotEvent.sortOrder,
                situationCode = shotEvent.situationCode,
                periodNumber = shotEvent.periodNumber,
                periodType = shotEvent.periodType,
                eventTypeName = shotEvent.eventTypeName,
                homeTeamDefendingSide = shotEvent.homeTeamDefendingSide,
                secondsIntoPeriod = shotEvent.secondsIntoPeriod,
                secondsLeftInPeriod = shotEvent.secondsLeftInPeriod,
                shooterTeamId = shotEvent.shooterTeamId,
                shooterPlayerId = shotEvent.shooterPlayerId,
                goalieId = shotEvent.goalieId,
                xCoordinate = shotEvent.xCoordinate,
                yCoordinate = shotEvent.yCoordinate,
                zone = shotEvent.zone,
                shotType = shotEvent.shotType
            };
        }
        public static IEnumerable<DbShot> MapList(IEnumerable<Shot> shotEvents, int gameId)
        {
            var dbEvents = new List<DbShot>();
            foreach (var shotEvent in shotEvents)
            {
                dbEvents.Add(Map(shotEvent, gameId));
            }

            return dbEvents;
        }
    }
}