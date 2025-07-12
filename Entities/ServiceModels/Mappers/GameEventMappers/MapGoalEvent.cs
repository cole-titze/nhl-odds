using Entities.Models.GamePlayEvents;
using Entities.Types;
using Entities.Types.Enums;

namespace Entities.ServiceModels.Mappers.GameEventMappers;

public static class MapGoalEvent
{
    /// <summary>
    /// Maps a goal event
    /// </summary>
    /// <param name="responseGameEvent">The event response from the NHL api</param>
    /// <returns>The goal event</returns>
    public static Goal Map(dynamic responseGameEvent)
    {
        string timeInPeriod = responseGameEvent.timeInPeriod;
        string timeLeftInPeriod = responseGameEvent.timeRemaining;

        return new Goal
        {
            Id = (int)responseGameEvent.eventId,
            TypeCode = (int)responseGameEvent.typeCode,
            SortOrder = (int)responseGameEvent.sortOrder,
            SituationCode = SituationCodeParser.ParseFromString((string)responseGameEvent.situationCode),
            PeriodNumber = (int)responseGameEvent.periodDescriptor.number,
            PeriodType = PeriodTypeParser.ParseFromString((string)responseGameEvent.periodDescriptor.periodType),
            EventTypeName = responseGameEvent.typeDescKey,
            HomeTeamDefendingSide = HomeTeamDefendingSideParser.ParseFromString((string)responseGameEvent.homeTeamDefendingSide),
            SecondsIntoPeriod = timeInPeriod.ParseIceTimeToSeconds(),
            SecondsLeftInPeriod = timeLeftInPeriod.ParseIceTimeToSeconds(),
            ShotType = ShotTypeParser.ParseFromString((string)responseGameEvent.details.shotType),
            ScoringPlayerTeamId = (int)responseGameEvent.details.eventOwnerTeamId,
            ScoringPlayerId = (int)responseGameEvent.details.scoringPlayerId,
            GoalieId = (int?)responseGameEvent.details.goalieInNetId,
            AssistOnePlayerId = (int?)responseGameEvent.details.assist1PlayerId,
            AssistTwoPlayerId = (int?)responseGameEvent.details.assist2PlayerId,
            Zone = ZoneParser.ParseFromString((string)responseGameEvent.details.zoneCode),
            XCoordinate = (int?)responseGameEvent.details.xCoord,
            YCoordinate = (int?)responseGameEvent.details.yCoord,
            HighlightClipSharingUrl = (string)responseGameEvent.details.highlightClipSharingUrl ?? "",
            HighlightClipId = (int?)responseGameEvent.details.highlightClip ?? -1,
            DiscreetClipId = (double?)responseGameEvent.details.discreteClip ?? -1,
            PptReplayUrl = (string)responseGameEvent.pptReplayUrl ?? "",
        };
    }
}