using Entities.DbModels.GamePlayEvents;
using Entities.Models.GamePlayEvents;

namespace Entities.Mappers.GameEventMappers;

public static class MapDbGoalEventToGoalEvent
{
    public static Goal Map(DbGoal db)
    {
        return new Goal
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
            XCoordinate = db.XCoordinate,
            YCoordinate = db.YCoordinate,
            Zone = db.Zone,
            ShotType = db.ShotType,
            ScoringPlayerTeamId = db.ScoringPlayerTeamId,
            AssistOnePlayerId = db.AssistOnePlayerId,
            AssistTwoPlayerId = db.AssistTwoPlayerId,
            ScoringPlayerId = db.ScoringPlayerId,
            GoalieId = db.GoalieId,
            HighlightClipSharingUrl = db.HighlightClipSharingUrl,
            HighlightClipId = db.HighlightClipId,
            DiscreetClipId = db.DiscreetClipId,
            PptReplayUrl = db.PptReplayUrl
        };
    }

    public static IEnumerable<Goal> MapList(IEnumerable<DbGoal> dbEvents)
    {
        var events = new List<Goal>();
        foreach (var db in dbEvents)
        {
            events.Add(Map(db));
        }
        return events;
    }
}
