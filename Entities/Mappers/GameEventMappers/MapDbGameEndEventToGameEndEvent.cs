using Entities.DbModels.GamePlayEvents;
using Entities.Models.GamePlayEvents;

namespace Entities.Mappers.GameEventMappers;

public static class MapDbGameEndEventToGameEndEvent
{
    public static GameEnd Map(DbGameEnd db)
    {
        return new GameEnd
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
            SecondsLeftInPeriod = db.SecondsLeftInPeriod
        };
    }

    public static IEnumerable<GameEnd> MapList(IEnumerable<DbGameEnd> dbEvents)
    {
        var events = new List<GameEnd>();
        foreach (var db in dbEvents)
        {
            events.Add(Map(db));
        }
        return events;
    }
}
