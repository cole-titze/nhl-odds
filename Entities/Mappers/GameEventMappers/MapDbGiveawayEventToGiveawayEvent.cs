using Entities.DbModels.GamePlayEvents;
using Entities.Models.GamePlayEvents;

namespace Entities.Mappers.GameEventMappers;

public static class MapDbGiveawayEventToGiveawayEvent
{
    public static Giveaway Map(DbGiveaway db)
    {
        return new Giveaway
        {
            Id = db.Id,
            TypeCode = db.TypeCode,
            SortOrder = db.SortOrder,
            SituationCode = db.SituationCode,
            PeriodNumber = db.PeriodNumber,
            PeriodType = db.PeriodType,
            EventTypeName = db.EventTypeName,
            HomeTeamDefendingSide = db.HomeTeamDefendingSide,
            SecondsIntoPeriod = db.SecondsIntoPeriod,
            SecondsLeftInPeriod = db.SecondsLeftInPeriod,
            GiveawayPlayerTeamId = db.GiveawayPlayerTeamId,
            GiveawayPlayerId = db.GiveawayPlayerId,
            XCoordinate = db.XCoordinate,
            YCoordinate = db.YCoordinate,
            Zone = db.Zone
        };
    }

    public static IEnumerable<Giveaway> MapList(IEnumerable<DbGiveaway> dbEvents)
    {
        var events = new List<Giveaway>();
        foreach (var db in dbEvents)
        {
            events.Add(Map(db));
        }
        return events;
    }
}