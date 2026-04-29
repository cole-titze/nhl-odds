using Entities.DbModels.GamePlayEvents;
using Entities.Models.GamePlayEvents;

namespace Entities.Mappers.GameEventMappers;

public static class MapDbPenaltyEventToPenaltyEvent
{
    public static Penalty Map(DbPenalty db)
    {
        return new Penalty
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
            CommittedByPlayerTeamId = db.CommittedByPlayerTeamId,
            XCoordinate = db.XCoordinate,
            YCoordinate = db.YCoordinate,
            CommittedByPlayerId = db.CommittedByPlayerId,
            DrawnByPlayerId = db.DrawnByPlayerId,
            ServedByPlayerId = db.ServedByPlayerId,
            Zone = db.Zone,
            Duration = db.Duration,
            PenaltyType = db.PenaltyType,
            PenaltySeverity = db.PenaltySeverity
        };
    }

    public static IEnumerable<Penalty> MapList(IEnumerable<DbPenalty> dbEvents)
    {
        var events = new List<Penalty>();
        foreach (var db in dbEvents)
        {
            events.Add(Map(db));
        }
        return events;
    }
}