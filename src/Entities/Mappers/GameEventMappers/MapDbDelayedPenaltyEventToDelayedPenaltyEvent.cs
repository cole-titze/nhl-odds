using Entities.DbModels.GamePlayEvents;
using Entities.Models.GamePlayEvents;

namespace Entities.Mappers.GameEventMappers;

public static class MapDbDelayedPenaltyEventToDelayedPenaltyEvent
{
    public static DelayedPenalty Map(DbDelayedPenalty db)
    {
        return new DelayedPenalty
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
            PenaltyTeamId = db.PenaltyTeamId
        };
    }

    public static IEnumerable<DelayedPenalty> MapList(IEnumerable<DbDelayedPenalty> dbEvents)
    {
        var events = new List<DelayedPenalty>();
        foreach (var db in dbEvents)
        {
            events.Add(Map(db));
        }
        return events;
    }
}
