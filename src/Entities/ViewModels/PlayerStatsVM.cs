using System.Text.Json.Serialization;

namespace Entities.ViewModels;

/// <summary>
/// A player's season for the MCP server and the assistant (GetPlayer): the Player page's data in a compact
/// form, with totals and per-game averages worked out so a model doesn't have to sum games. Skater fields
/// are left out for goalies and goalie fields for skaters.
/// </summary>
public class PlayerStatsVM
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Position { get; set; }
    public short? SweaterNumber { get; set; }
    public string? BirthDate { get; set; }
    // The team they're on now (the NHL's roster report); null when on no NHL roster
    public int? CurrentTeamId { get; set; }
    public string? CurrentTeam { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsInjuredReserve { get; set; }
    // The team and season of their latest game, when it isn't CurrentTeam this season
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LastPlayedFor { get; set; }
    public int SeasonStartYear { get; set; }
    public PlayerStatsLineVM SeasonTotals { get; set; } = new();
    public PlayerStatsLineVM? PerGameAverages { get; set; }
    // Newest first
    public List<PlayerStatsGameVM> Games { get; set; } = new();
}

/// <summary>Totals or per-game averages: skater fields for skaters, goalie fields for goalies.</summary>
public class PlayerStatsLineVM
{
    public int GamesPlayed { get; set; }
    public string TimeOnIce { get; set; } = string.Empty;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public double? Goals { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public double? Assists { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public double? Points { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public double? ShotsOnGoal { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public double? PlusMinus { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public double? ShotsAgainst { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public double? Saves { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public double? GoalsAgainst { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public double? SavePct { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public double? GoalsAgainstAverage { get; set; }
}

/// <summary>One game in a compact row.</summary>
public class PlayerStatsGameVM
{
    public int GameId { get; set; }
    // yyyy-MM-dd, US Central time like the site's game days
    public string Date { get; set; } = string.Empty;
    public bool Playoff { get; set; }
    public int OpponentId { get; set; }
    public string Opponent { get; set; } = string.Empty;
    public bool Home { get; set; }
    // "W", "L" or "OTL", then his team's score first, e.g. "W 4-1"
    public string Result { get; set; } = string.Empty;
    public string TimeOnIce { get; set; } = string.Empty;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public int? Goals { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public int? Assists { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public int? ShotsOnGoal { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public int? PlusMinus { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public int? ShotsAgainst { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public int? Saves { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public int? GoalsAgainst { get; set; }
}
