using Entities.DbModels.GamePlayEvents;
using Entities.Models.GamePlayEvents;

namespace Entities.Mappers.GameEventMappers;

public static class MapDbBlockedShotEventToBlockedShotEvent
{
    public static BlockedShot Map(DbBlockedShot db)
    {
        return new BlockedShot
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
            BlockingPlayerTeamId = db.BlockingPlayerTeamId,
            BlockingPlayerId = db.BlockingPlayerId,
            ShooterPlayerId = db.ShooterPlayerId,
            XCoordinate = db.XCoordinate,
            YCoordinate = db.YCoordinate,
            Zone = db.Zone,
            BlockType = db.BlockType
        };
    }

    public static IEnumerable<BlockedShot> MapList(IEnumerable<DbBlockedShot> dbEvents)
    {
        var events = new List<BlockedShot>();
        foreach (var db in dbEvents)
        {
            events.Add(Map(db));
        }
        return events;
    }
}