using Entities.DbModels.GamePlayEvents;
using Entities.Models.GamePlayEvents;

namespace Entities.Mappers.GameEventMappers;

public static class MapGoalEventToDbGoalEvent
{
    public static DbGoal Map(Goal goalEvent, int gameId)
    {
        return new DbGoal
        {
            Id = goalEvent.Id,
            GameId = gameId,
            TypeCode = goalEvent.TypeCode,
            SortOrder = goalEvent.SortOrder,
            SituationCode = goalEvent.SituationCode,
            PeriodNumber = goalEvent.PeriodNumber,
            PeriodType = goalEvent.PeriodType,
            EventTypeName = goalEvent.EventTypeName,
            HomeTeamDefendingSide = goalEvent.HomeTeamDefendingSide,
            SecondsIntoPeriod = goalEvent.SecondsIntoPeriod,
            SecondsLeftInPeriod = goalEvent.SecondsLeftInPeriod,
            XCoordinate = goalEvent.XCoordinate,
            YCoordinate = goalEvent.YCoordinate,
            Zone = goalEvent.Zone,
            ShotType = goalEvent.ShotType,
            ScoringPlayerTeamId = goalEvent.ScoringPlayerTeamId,
            AssistOnePlayerId = goalEvent.AssistOnePlayerId,
            AssistTwoPlayerId = goalEvent.AssistTwoPlayerId,
            ScoringPlayerId = goalEvent.ScoringPlayerId,
            GoalieId = goalEvent.GoalieId,
            HighlightClipSharingUrl = goalEvent.HighlightClipSharingUrl,
            HighlightClipId = goalEvent.HighlightClipId,
            DiscreetClipId = goalEvent.DiscreetClipId,
            PptReplayUrl = goalEvent.PptReplayUrl
        };
    }

    public static IEnumerable<DbGoal> MapList(IEnumerable<Goal> goalEvents, int gameId)
    {
        var dbEvents = new List<DbGoal>();
        foreach (var goalEvent in goalEvents)
        {
            dbEvents.Add(Map(goalEvent, gameId));
        }

        return dbEvents;
    }
}