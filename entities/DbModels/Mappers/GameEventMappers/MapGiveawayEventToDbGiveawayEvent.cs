using Entities.DbModels.GamePlayEvents;
using Entities.Models.GamePlayEvents;

namespace Entities.DbModels.Mappers.GameEventMappers;

public static class MapGiveawayEventToDbGiveawayEvent
{
    public static DbGiveaway Map(Giveaway giveawayEvent, int gameId)
    {
        return new DbGiveaway
        {
            Id = giveawayEvent.Id,
            GameId = gameId,
            TypeCode = giveawayEvent.TypeCode,
            SortOrder = giveawayEvent.SortOrder,
            SituationCode = giveawayEvent.SituationCode,
            PeriodNumber = giveawayEvent.PeriodNumber,
            PeriodType = giveawayEvent.PeriodType,
            EventTypeName = giveawayEvent.EventTypeName,
            HomeTeamDefendingSide = giveawayEvent.HomeTeamDefendingSide,
            SecondsIntoPeriod = giveawayEvent.SecondsIntoPeriod,
            SecondsLeftInPeriod = giveawayEvent.SecondsLeftInPeriod,
            GiveawayPlayerTeamId = giveawayEvent.GiveawayPlayerTeamId,
            GiveawayPlayerId = giveawayEvent.GiveawayPlayerId,
            XCoordinate = giveawayEvent.XCoordinate,
            YCoordinate = giveawayEvent.YCoordinate,
            Zone = giveawayEvent.Zone
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