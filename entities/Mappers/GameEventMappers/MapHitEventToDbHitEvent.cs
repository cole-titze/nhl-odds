using Entities.DbModels.GamePlayEvents;
using Entities.Models.GamePlayEvents;

namespace Entities.Mappers.GameEventMappers;

public static class MapHitEventToDbHitEvent
{
    public static DbHit Map(Hit hitEvent, int gameId)
    {
        return new DbHit
        {
            Id = hitEvent.Id,
            GameId = gameId,
            TypeCode = hitEvent.TypeCode,
            SortOrder = hitEvent.SortOrder,
            SituationCode = hitEvent.SituationCode,
            PeriodNumber = hitEvent.PeriodNumber,
            PeriodType = hitEvent.PeriodType,
            EventTypeName = hitEvent.EventTypeName,
            HomeTeamDefendingSide = hitEvent.HomeTeamDefendingSide,
            SecondsIntoPeriod = hitEvent.SecondsIntoPeriod,
            SecondsLeftInPeriod = hitEvent.SecondsLeftInPeriod,
            HittingPlayerTeamId = hitEvent.HittingPlayerTeamId,
            HittingPlayerId = hitEvent.HittingPlayerId,
            HitteePlayerId = hitEvent.HitteePlayerId,
            XCoordinate = hitEvent.XCoordinate,
            YCoordinate = hitEvent.YCoordinate,
            Zone = hitEvent.Zone
        };
    }

    public static IEnumerable<DbHit> MapList(IEnumerable<Hit> hitEvents, int gameId)
    {
        var dbEvents = new List<DbHit>();
        foreach (var hitEvent in hitEvents)
        {
            dbEvents.Add(Map(hitEvent, gameId));
        }

        return dbEvents;
    }
}