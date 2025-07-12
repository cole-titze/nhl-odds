using Entities.DbModels.GamePlayEvents;
using Entities.Models.GamePlayEvents;

namespace Entities.Mappers.GameEventMappers;

public static class MapDelayedPenaltyEventToDbDelayedPenaltyEvent
{
    public static DbDelayedPenalty Map(DelayedPenalty delayedPenaltyEvent, int gameId)
    {
        return new DbDelayedPenalty
        {
            Id = delayedPenaltyEvent.Id,
            GameId = gameId,
            TypeCode = delayedPenaltyEvent.TypeCode,
            SortOrder = delayedPenaltyEvent.SortOrder,
            SituationCode = delayedPenaltyEvent.SituationCode,
            PeriodNumber = delayedPenaltyEvent.PeriodNumber,
            PeriodType = delayedPenaltyEvent.PeriodType,
            EventTypeName = delayedPenaltyEvent.EventTypeName,
            HomeTeamDefendingSide = delayedPenaltyEvent.HomeTeamDefendingSide,
            SecondsIntoPeriod = delayedPenaltyEvent.SecondsIntoPeriod,
            SecondsLeftInPeriod = delayedPenaltyEvent.SecondsLeftInPeriod,
            PenaltyTeamId = delayedPenaltyEvent.PenaltyTeamId
        };
    }

    public static IEnumerable<DbDelayedPenalty> MapList(IEnumerable<DelayedPenalty> delayedPenaltyEvents, int gameId)
    {
        var dbEvents = new List<DbDelayedPenalty>();
        foreach (var delayedPenaltyEvent in delayedPenaltyEvents)
        {
            dbEvents.Add(Map(delayedPenaltyEvent, gameId));
        }

        return dbEvents;
    }
}