using Entities.DbModels.GamePlayEvents;
using Entities.Models.GamePlayEvents;

namespace Entities.DbModels.Mappers.GameEventMappers;

public static class MapMissedShotEventToDbMissedShotEvent
{
    public static DbMissedShot Map(MissedShot missedShotEvent, int gameId)
    {
        return new DbMissedShot
        {
            Id = missedShotEvent.Id,
            GameId = gameId,
            TypeCode = missedShotEvent.TypeCode,
            SortOrder = missedShotEvent.SortOrder,
            SituationCode = missedShotEvent.SituationCode,
            PeriodNumber = missedShotEvent.PeriodNumber,
            PeriodType = missedShotEvent.PeriodType,
            EventTypeName = missedShotEvent.EventTypeName,
            HomeTeamDefendingSide = missedShotEvent.HomeTeamDefendingSide,
            ShotType = missedShotEvent.ShotType,
            SecondsIntoPeriod = missedShotEvent.SecondsIntoPeriod,
            SecondsLeftInPeriod = missedShotEvent.SecondsLeftInPeriod,
            ShootingTeamId = missedShotEvent.ShootingTeamId,
            ShootingPlayerId = missedShotEvent.ShootingPlayerId,
            GoalieId = missedShotEvent.GoalieId,
            XCoordinate = missedShotEvent.XCoordinate,
            YCoordinate = missedShotEvent.YCoordinate,
            Zone = missedShotEvent.Zone,
            MissType = missedShotEvent.MissType
        };
    }

    public static IEnumerable<DbMissedShot> MapList(IEnumerable<MissedShot> missedShotEvents, int gameId)
    {
        var dbEvents = new List<DbMissedShot>();
        foreach (var missedShotEvent in missedShotEvents)
        {
            dbEvents.Add(Map(missedShotEvent, gameId));
        }

        return dbEvents;
    }
}