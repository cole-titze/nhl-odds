using System.Text.Json.Nodes;
using Entities.Models.GamePlayEvents;
using Entities.Types;
using Entities.Types.Enums;

namespace Entities.ServiceModels.Mappers.GameEventMappers;

public static class MapMissedShotEvent
{
    public static MissedShot Map(JsonNode responseGameEvent)
    {
        string timeInPeriod = responseGameEvent["timeInPeriod"]!.GetValue<string>();
        string timeLeftInPeriod = responseGameEvent["timeRemaining"]!.GetValue<string>();

        return new MissedShot
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
            ShotType = ShotTypeParser.ParseFromString(responseGameEvent["details"]!["shotType"]!.GetValue<string>()),
            ShootingTeamId = responseGameEvent["details"]!["eventOwnerTeamId"]!.GetValue<int>(),
            ShootingPlayerId = responseGameEvent["details"]!["shootingPlayerId"]!.GetValue<int>(),
            // Some old games use goalie id 0 for an empty net; there is no player 0
            GoalieId = responseGameEvent["details"]!["goalieInNetId"]?.GetValue<int>() is int goalieId && goalieId != 0 ? goalieId : null,
            Zone = ZoneParser.ParseFromString(responseGameEvent["details"]!["zoneCode"]!.GetValue<string>()),
            XCoordinate = responseGameEvent["details"]!["xCoord"]?.GetValue<int>(),
            YCoordinate = responseGameEvent["details"]!["yCoord"]?.GetValue<int>(),
            MissType = MissedShotTypeParser.ParseFromString(responseGameEvent["details"]!["reason"]!.GetValue<string>())
        };
    }
}