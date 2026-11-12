namespace Entities.ViewModels;

/// <summary>The league leaders in one stat for a regular season (#182): the Leaders page and the MCP tool.</summary>
public class StatLeadersVM
{
    public int SeasonStartYear { get; set; }
    // The stat's key, e.g. "points" or "savePercentage"
    public string Stat { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    // "skater" or "goalie"
    public string Group { get; set; } = string.Empty;
    // Goals against average: the lowest leads
    public bool LowerIsBetter { get; set; }
    // Rate stats (per game, percentages) only count players with at least this many games; 0 for totals
    public int MinGamesPlayed { get; set; }
    public List<StatLeaderVM> Leaders { get; set; } = [];
}

public class StatLeaderVM
{
    // Shared on ties (1, 2, 2, 4)
    public int Rank { get; set; }
    public int PlayerId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string HeadShot { get; set; } = string.Empty;
    // "C", "L", "R", "D" or "G"
    public string Position { get; set; } = string.Empty;
    // The team of their latest game that season
    public int TeamId { get; set; }
    public string TeamAbbreviation { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public string TeamLogoUri { get; set; } = string.Empty;
    public int GamesPlayed { get; set; }
    // Counts are whole numbers; time on ice per game is in seconds, save percentage a fraction (0.915)
    public double Value { get; set; }
}
