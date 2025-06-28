using Entities.DbModels.GamePlayEvents;
using Entities.Models.GamePlayEvents;

namespace Entities.DbModels.Mappers.GameEventMappers;

public static class MapGameEndEventToDbGameEndEvent
{
    public static DbGameEnd Map(GameEnd gameEndEvent, int gameId)
    {
        return new DbGameEnd
        {
            Id = gameEndEvent.Id,
            GameId = gameId,
            TypeCode = gameEndEvent.TypeCode,
            SortOrder = gameEndEvent.SortOrder,
            SituationCode = gameEndEvent.SituationCode,
            PeriodNumber = gameEndEvent.PeriodNumber,
            PeriodType = gameEndEvent.PeriodType,
            EventTypeName = gameEndEvent.EventTypeName,
            HomeTeamDefendingSide = gameEndEvent.HomeTeamDefendingSide,
            SecondsIntoPeriod = gameEndEvent.SecondsIntoPeriod,
            SecondsLeftInPeriod = gameEndEvent.SecondsLeftInPeriod
        };
    }

    public static IEnumerable<DbGameEnd> MapList(IEnumerable<GameEnd> gameEndEvents, int gameId)
    {
        var dbEvents = new List<DbGameEnd>();
        foreach (var gameEndEvent in gameEndEvents)
        {
            dbEvents.Add(Map(gameEndEvent, gameId));
        }

        return dbEvents;
    }
}