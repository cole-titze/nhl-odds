namespace Entities.ViewModels;

/// <summary>A box score line: the skater fields for skaters, the goalie fields for goalies.</summary>
public class PlayerGameStatsVM
{
    public int TimeOnIceSeconds { get; set; }
    public int Goals { get; set; }
    public int Assists { get; set; }
    public int Points { get; set; }
    public int ShotsOnGoal { get; set; }
    public int PlusMinus { get; set; }
    public int ShotsAgainst { get; set; }
    public int Saves { get; set; }
    public int GoalsAgainst { get; set; }
    // Saves / shots against; null with no shots against
    public double? SavePct { get; set; }
    public bool IsStarter { get; set; }
}
