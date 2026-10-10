using System.Text.Json;
using Entities.DbModels;

namespace DataGetter.BusinessLogic;

/// <summary>Reads game rosters (play-by-play rosterSpots), shift charts and goal tracking replays into rows.</summary>
public static class GameDetailParser
{
    public static List<DbGameRosterSpot> ParseRosterSpots(int gameId, string playByPlayJson)
    {
        using var doc = JsonDocument.Parse(playByPlayJson);
        if (!doc.RootElement.TryGetProperty("rosterSpots", out var spots))
            return [];
        return spots.EnumerateArray()
            .Select(s => new DbGameRosterSpot
            {
                GameId = gameId,
                PlayerId = s.GetProperty("playerId").GetInt32(),
                TeamId = s.GetProperty("teamId").GetInt32(),
                SweaterNumber = ReadShort(s, "sweaterNumber"),
                PositionCode = s.TryGetProperty("positionCode", out var p) ? p.GetString() ?? string.Empty : string.Empty,
            })
            .DistinctBy(s => s.PlayerId)
            .ToList();
    }

    public static List<DbGameShift> ParseShifts(int gameId, string shiftChartJson)
    {
        using var doc = JsonDocument.Parse(shiftChartJson);
        return doc.RootElement.GetProperty("data").EnumerateArray()
            .Select(s => new DbGameShift
            {
                Id = s.GetProperty("id").GetInt64(),
                GameId = gameId,
                PlayerId = s.GetProperty("playerId").GetInt32(),
                TeamId = s.GetProperty("teamId").GetInt32(),
                Period = (short)s.GetProperty("period").GetInt32(),
                ShiftNumber = (short)s.GetProperty("shiftNumber").GetInt32(),
                StartSeconds = Seconds(ReadString(s, "startTime")) ?? 0,
                EndSeconds = Seconds(ReadString(s, "endTime")) ?? 0,
                DurationSeconds = Seconds(ReadString(s, "duration")),
                TypeCode = (short)s.GetProperty("typeCode").GetInt32(),
                DetailCode = (short)(s.TryGetProperty("detailCode", out var d) && d.ValueKind == JsonValueKind.Number ? d.GetInt32() : 0),
                EventNumber = s.TryGetProperty("eventNumber", out var e) && e.ValueKind == JsonValueKind.Number ? e.GetInt32() : null,
                EventDescription = ReadString(s, "eventDescription"),
                EventDetails = ReadString(s, "eventDetails"),
            })
            .DistinctBy(s => s.Id)
            .ToList();
    }

    /// <summary>Every tracked object in every frame. The puck has empty player, team and sweater fields.</summary>
    public static List<DbGoalReplayPosition> ParseReplay(int gameId, int eventId, string replayJson)
    {
        using var doc = JsonDocument.Parse(replayJson);
        var positions = new List<DbGoalReplayPosition>();
        short frame = 0;
        foreach (var f in doc.RootElement.EnumerateArray())
        {
            var timeStamp = f.GetProperty("timeStamp").GetInt64();
            foreach (var tracked in f.GetProperty("onIce").EnumerateObject())
            {
                var o = tracked.Value;
                // Some frames carry empty {} entries
                if (!o.TryGetProperty("id", out _) || !o.TryGetProperty("x", out _) || !o.TryGetProperty("y", out _))
                    continue;
                positions.Add(new DbGoalReplayPosition
                {
                    GameId = gameId,
                    EventId = eventId,
                    Frame = frame,
                    TrackId = o.GetProperty("id").GetInt32(),
                    TimeStamp = timeStamp,
                    PlayerId = ReadInt(o, "playerId"),
                    TeamId = ReadInt(o, "teamId"),
                    SweaterNumber = ReadShort(o, "sweaterNumber"),
                    X = o.GetProperty("x").GetSingle(),
                    Y = o.GetProperty("y").GetSingle(),
                });
            }
            frame++;
        }
        return positions.DistinctBy(p => (p.Frame, p.TrackId)).ToList();
    }

    /// <summary>"12:34" → 754; null when missing or not a time.</summary>
    public static short? Seconds(string? time)
    {
        if (string.IsNullOrEmpty(time))
            return null;
        var parts = time.Split(':');
        return parts.Length == 2 && int.TryParse(parts[0], out var minutes) && int.TryParse(parts[1], out var seconds)
            ? (short)(minutes * 60 + seconds)
            : null;
    }

    private static string? ReadString(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }

    // Numbers, or numbers sent as strings; "" and null are null
    private static int? ReadInt(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value))
            return null;
        if (value.ValueKind == JsonValueKind.Number)
            return value.GetInt32();
        return value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out var parsed) ? parsed : null;
    }

    private static short? ReadShort(JsonElement element, string name)
    {
        return ReadInt(element, name) is int value ? (short)value : null;
    }
}