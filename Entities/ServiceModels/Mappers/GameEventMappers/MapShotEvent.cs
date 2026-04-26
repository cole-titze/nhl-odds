using System.Text.Json.Nodes;
using Entities.Models.GamePlayEvents;
using Entities.Types;
using Entities.Types.Enums;

namespace Entities.ServiceModels.Mappers.GameEventMappers;

public static class MapShotEvent
{
    public static Shot Map(JsonNode responseGameEvent)
    {
        string timeInPeriod = responseGameEvent["timeInPeriod"]!.GetValue<string>();
        string timeLeftInPeriod = responseGameEvent["timeRemaining"]!.GetValue<string>();

        return new Shot
        {
            Id = responseGameEvent["eventId"]!.GetValue<int>(),
            TypeCode = responseGameEvent["typeCode"]!.GetValue<int>(),
            SortOrder = responseGameEvent["sortOrder"]!.GetValue<int>(),
            SituationCode = responseGameEvent["situationCode"]?.GetCoercedInt() ?? 1551,
            PeriodNumber = responseGameEvent["periodDescriptor"]!["number"]!.GetValue<int>(),
            PeriodType = PeriodTypeParser.ParseFromString(responseGameEvent["periodDescriptor"]!["periodType"]!.GetValue<string>()),
            EventTypeName = responseGameEvent["typeDescKey"]!.GetValue<string>(),
            HomeTeamDefendingSide = HomeTeamDefendingSideParser.ParseFromString(responseGameEvent["homeTeamDefendingSide"]!.GetValue<string>()),
            SecondsIntoPeriod = timeInPeriod.ParseIceTimeToSeconds(),
            SecondsLeftInPeriod = timeLeftInPeriod.ParseIceTimeToSeconds(),
            ShotType = ShotTypeParser.ParseFromString(responseGameEvent["details"]!["shotType"]!.GetValue<string>()),
            Zone = ZoneParser.ParseFromString(responseGameEvent["details"]!["zoneCode"]!.GetValue<string>()),
            ShooterTeamId = responseGameEvent["details"]!["eventOwnerTeamId"]!.GetValue<int>(),
            ShooterPlayerId = responseGameEvent["details"]!["shootingPlayerId"]!.GetValue<int>(),
            GoalieId = responseGameEvent["details"]!["goalieInNetId"]?.GetValue<int>(),
            XCoordinate = responseGameEvent["details"]!["xCoord"]?.GetValue<int>(),
            YCoordinate = responseGameEvent["details"]!["yCoord"]?.GetValue<int>(),
        };
    }
}