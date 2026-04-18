using System.Text.Json.Nodes;
using Entities.Models.GamePlayEvents;
using Entities.Types;
using Entities.Types.Enums;

namespace Entities.ServiceModels.Mappers.GameEventMappers;

public static class MapGoalEvent
{
    public static Goal Map(JsonNode responseGameEvent)
    {
        string timeInPeriod = responseGameEvent["timeInPeriod"]!.GetValue<string>();
        string timeLeftInPeriod = responseGameEvent["timeRemaining"]!.GetValue<string>();

        return new Goal
        {
            Id = responseGameEvent["eventId"]!.GetValue<int>(),
            TypeCode = responseGameEvent["typeCode"]!.GetValue<int>(),
            SortOrder = responseGameEvent["sortOrder"]!.GetValue<int>(),
            SituationCode = responseGameEvent["situationCode"]?.GetCoercedInt() ?? -1,
            PeriodNumber = responseGameEvent["periodDescriptor"]!["number"]!.GetValue<int>(),
            PeriodType = PeriodTypeParser.ParseFromString(responseGameEvent["periodDescriptor"]!["periodType"]!.GetValue<string>()),
            EventTypeName = responseGameEvent["typeDescKey"]!.GetValue<string>(),
            HomeTeamDefendingSide = HomeTeamDefendingSideParser.ParseFromString(responseGameEvent["homeTeamDefendingSide"]?.GetValue<string>()),
            SecondsIntoPeriod = timeInPeriod.ParseIceTimeToSeconds(),
            SecondsLeftInPeriod = timeLeftInPeriod.ParseIceTimeToSeconds(),
            ShotType = ShotTypeParser.ParseFromString(responseGameEvent["details"]!["shotType"]?.GetValue<string>()),
            ScoringPlayerTeamId = responseGameEvent["details"]!["eventOwnerTeamId"]!.GetValue<int>(),
            ScoringPlayerId = responseGameEvent["details"]!["scoringPlayerId"]!.GetValue<int>(),
            GoalieId = responseGameEvent["details"]!["goalieInNetId"]?.GetValue<int>(),
            AssistOnePlayerId = responseGameEvent["details"]!["assist1PlayerId"]?.GetValue<int>(),
            AssistTwoPlayerId = responseGameEvent["details"]!["assist2PlayerId"]?.GetValue<int>(),
            Zone = ZoneParser.ParseFromString(responseGameEvent["details"]!["zoneCode"]?.GetValue<string>()),
            XCoordinate = responseGameEvent["details"]!["xCoord"]?.GetValue<int>(),
            YCoordinate = responseGameEvent["details"]!["yCoord"]?.GetValue<int>(),
            HighlightClipSharingUrl = responseGameEvent["details"]!["highlightClipSharingUrl"]?.GetValue<string>() ?? "",
            HighlightClipId = responseGameEvent["details"]!["highlightClip"]?.GetValue<long>() ?? -1,
            DiscreetClipId = responseGameEvent["details"]!["discreteClip"]?.GetValue<long>() ?? -1,
            PptReplayUrl = responseGameEvent["pptReplayUrl"]?.GetValue<string>() ?? "",
        };
    }
}
