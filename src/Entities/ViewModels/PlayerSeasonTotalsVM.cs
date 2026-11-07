namespace Entities.ViewModels;

/// <summary>A player's totals over the season's games on his page.</summary>
public class PlayerSeasonTotalsVM
{
    public int GamesPlayed { get; set; }
    public int TimeOnIceSeconds { get; set; }
    public int Goals { get; set; }
    public int Assists { get; set; }
    public int Points { get; set; }
    public int ShotsOnGoal { get; set; }
    public int PlusMinus { get; set; }
    public int ShotsAgainst { get; set; }
    public int Saves { get; set; }
    public int GoalsAgainst { get; set; }
    // null with no shots against
    public double? SavePct { get; set; }
    // Goals against per 60 minutes; null with no time on ice
    public double? GoalsAgainstAverage { get; set; }
}
