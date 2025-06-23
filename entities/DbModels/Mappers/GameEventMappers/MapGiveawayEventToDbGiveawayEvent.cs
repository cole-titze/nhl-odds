using Entities.DbModels.GamePlayEvents;
using Entities.Models.GamePlayEvents;

namespace Entities.DbModels.Mappers.GameEventMappers
{
    public static class MapGiveawayEventToDbGiveawayEvent
    {
        public static DbGiveaway Map(Giveaway giveawayEvent, int gameId)
        {
            return new DbGiveaway
            {
                id = giveawayEvent.id,
                gameId = gameId,
                typeCode = giveawayEvent.typeCode,
                sortOrder = giveawayEvent.sortOrder,
                situationCode = giveawayEvent.situationCode,
                periodNumber = giveawayEvent.periodNumber,
                periodType = giveawayEvent.periodType,
                eventTypeName = giveawayEvent.eventTypeName,
                homeTeamDefendingSide = giveawayEvent.homeTeamDefendingSide,
                secondsIntoPeriod = giveawayEvent.secondsIntoPeriod,
                secondsLeftInPeriod = giveawayEvent.secondsLeftInPeriod,
                giveawayPlayerTeamId = giveawayEvent.giveawayPlayerTeamId,
                giveawayPlayerId = giveawayEvent.giveawayPlayerId,
                xCoordinate = giveawayEvent.xCoordinate,
                yCoordinate = giveawayEvent.yCoordinate,
                zone = giveawayEvent.zone
            };
        }

        public static IEnumerable<DbGiveaway> MapList(IEnumerable<Giveaway> giveawayEvents, int gameId)
        {
            var dbEvents = new List<DbGiveaway>();
            foreach (var giveawayEvent in giveawayEvents)
            {
                dbEvents.Add(Map(giveawayEvent, gameId));
            }

            return dbEvents;
        }
    }
}