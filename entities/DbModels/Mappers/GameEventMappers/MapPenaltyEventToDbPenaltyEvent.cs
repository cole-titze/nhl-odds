using Entities.DbModels.GamePlayEvents;
using Entities.Models.GamePlayEvents;

namespace Entities.DbModels.Mappers.GameEventMappers;

public static class MapPenaltyEventToDbPenaltyEvent
{
    public static DbPenalty Map(Penalty penaltyEvent, int gameId)
    {
        return new DbPenalty
        {
            Id = penaltyEvent.Id,
            GameId = gameId,
            TypeCode = penaltyEvent.TypeCode,
            SortOrder = penaltyEvent.SortOrder,
            SituationCode = penaltyEvent.SituationCode,
            PeriodNumber = penaltyEvent.PeriodNumber,
            PeriodType = penaltyEvent.PeriodType,
            EventTypeName = penaltyEvent.EventTypeName,
            HomeTeamDefendingSide = penaltyEvent.HomeTeamDefendingSide,
            SecondsIntoPeriod = penaltyEvent.SecondsIntoPeriod,
            SecondsLeftInPeriod = penaltyEvent.SecondsLeftInPeriod,
            CommittedByPlayerTeamId = penaltyEvent.CommittedByPlayerTeamId,
            DrawnByPlayerId = penaltyEvent.DrawnByPlayerId,
            CommittedByPlayerId = penaltyEvent.CommittedByPlayerId,
            ServedByPlayerId = penaltyEvent.ServedByPlayerId,
            XCoordinate = penaltyEvent.XCoordinate,
            YCoordinate = penaltyEvent.YCoordinate,
            Zone = penaltyEvent.Zone,
            Duration = penaltyEvent.Duration,
            PenaltyType = penaltyEvent.PenaltyType,
            PenaltySeverity = penaltyEvent.PenaltySeverity
        };
    }

    public static IEnumerable<DbPenalty> MapList(IEnumerable<Penalty> penaltyEvents, int gameId)
    {
        var dbEvents = new List<DbPenalty>();
        foreach (var penaltyEvent in penaltyEvents)
        {
            dbEvents.Add(Map(penaltyEvent, gameId));
        }

        return dbEvents;
    }
}