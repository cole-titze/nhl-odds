using Entities.DbModels.GamePlayEvents;
using Entities.Models.GamePlayEvents;

namespace Entities.Mappers.GameEventMappers;

public static class MapShotEventToDbShotEvent
{
    public static DbShot Map(Shot shotEvent, int gameId)
    {
        return new DbShot
        {
            Id = shotEvent.Id,
            GameId = gameId,
            TypeCode = shotEvent.TypeCode,
            SortOrder = shotEvent.SortOrder,
            SituationCode = shotEvent.SituationCode,
            PeriodNumber = shotEvent.PeriodNumber,
            PeriodType = shotEvent.PeriodType,
            EventTypeName = shotEvent.EventTypeName,
            HomeTeamDefendingSide = shotEvent.HomeTeamDefendingSide,
            SecondsIntoPeriod = shotEvent.SecondsIntoPeriod,
            SecondsLeftInPeriod = shotEvent.SecondsLeftInPeriod,
            ShootingTeamId = shotEvent.ShooterTeamId,
            ShootingPlayerId = shotEvent.ShooterPlayerId,
            GoalieId = shotEvent.GoalieId,
            XCoordinate = shotEvent.XCoordinate,
            YCoordinate = shotEvent.YCoordinate,
            Zone = shotEvent.Zone,
            ShotType = shotEvent.ShotType
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