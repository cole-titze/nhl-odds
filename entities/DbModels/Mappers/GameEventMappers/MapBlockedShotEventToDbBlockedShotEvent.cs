using Entities.DbModels.GamePlayEvents;
using Entities.Models.GamePlayEvents;

namespace Entities.DbModels.Mappers.GameEventMappers;

public static class MapBlockedShotEventToDbBlockedShotEvent
{
    public static DbBlockedShot Map(BlockedShot blockedShotEvent, int gameId)
    {
        return new DbBlockedShot
        {
            Id = blockedShotEvent.Id,
            GameId = gameId,
            TypeCode = blockedShotEvent.TypeCode,
            SortOrder = blockedShotEvent.SortOrder,
            SituationCode = blockedShotEvent.SituationCode,
            PeriodNumber = blockedShotEvent.PeriodNumber,
            PeriodType = blockedShotEvent.PeriodType,
            EventTypeName = blockedShotEvent.EventTypeName,
            HomeTeamDefendingSide = blockedShotEvent.HomeTeamDefendingSide,
            SecondsIntoPeriod = blockedShotEvent.SecondsIntoPeriod,
            SecondsLeftInPeriod = blockedShotEvent.SecondsLeftInPeriod,
            BlockingPlayerTeamId = blockedShotEvent.BlockingPlayerTeamId,
            BlockingPlayerId = blockedShotEvent.BlockingPlayerId,
            ShooterPlayerId = blockedShotEvent.ShooterPlayerId,
            XCoordinate = blockedShotEvent.XCoordinate,
            YCoordinate = blockedShotEvent.YCoordinate,
            Zone = blockedShotEvent.Zone,
            BlockType = blockedShotEvent.BlockType
        };
    }

    public static IEnumerable<DbBlockedShot> MapList(IEnumerable<BlockedShot> blockedShotEvents, int gameId)
    {
        var dbEvents = new List<DbBlockedShot>();
        foreach (var blockedShotEvent in blockedShotEvents)
        {
            dbEvents.Add(Map(blockedShotEvent, gameId));
        }

        return dbEvents;
    }
}