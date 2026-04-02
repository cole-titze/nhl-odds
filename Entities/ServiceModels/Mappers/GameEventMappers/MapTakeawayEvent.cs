using System.Text.Json.Nodes;
using Entities.Models.GamePlayEvents;
using Entities.Types;
using Entities.Types.Enums;

namespace Entities.ServiceModels.Mappers.GameEventMappers;

public static class MapTakeawayEvent
{
    public static Takeaway Map(JsonNode responseGameEvent)
    {
        string timeInPeriod = responseGameEvent["timeInPeriod"]!.GetValue<string>();
        string timeLeftInPeriod = responseGameEvent["timeRemaining"]!.GetValue<string>();

        return new Takeaway
        {
            Id = responseGameEvent["eventId"]!.GetValue<int>(),
            TypeCode = responseGameEvent["typeCode"]!.GetValue<int>(),
            SortOrder = responseGameEvent["sortOrder"]!.GetValue<int>(),
            SituationCode = SituationCodeParser.ParseFromString(responseGameEvent["situationCode"]!.GetValue<string>()),
            PeriodNumber = responseGameEvent["periodDescriptor"]!["number"]!.GetValue<int>(),
            PeriodType = PeriodTypeParser.ParseFromString(responseGameEvent["periodDescriptor"]!["periodType"]!.GetValue<string>()),
            EventTypeName = responseGameEvent["typeDescKey"]!.GetValue<string>(),
            HomeTeamDefendingSide = HomeTeamDefendingSideParser.ParseFromString(responseGameEvent["homeTeamDefendingSide"]!.GetValue<string>()),
            SecondsIntoPeriod = timeInPeriod.ParseIceTimeToSeconds(),
            SecondsLeftInPeriod = timeLeftInPeriod.ParseIceTimeToSeconds(),
            TakeawayPlayerTeamId = responseGameEvent["details"]!["eventOwnerTeamId"]!.GetValue<int>(),
            TakeawayPlayerId = responseGameEvent["details"]!["playerId"]!.GetValue<int>(),
            Zone = ZoneParser.ParseFromString(responseGameEvent["details"]!["zoneCode"]!.GetValue<string>()),
            XCoordinate = responseGameEvent["details"]!["xCoord"]?.GetValue<int>(),
            YCoordinate = responseGameEvent["details"]!["yCoord"]?.GetValue<int>(),
        };
    }
}
