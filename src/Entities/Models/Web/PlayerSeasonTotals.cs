using Entities.Types;

namespace Entities.Models.Web;

/// <summary>
/// One player's regular-season totals from the box scores, for the stat leaders (#182). Skaters fill the skater
/// fields, goalies the goalie ones. Team and position are from the player's latest game that season.
/// </summary>
public class PlayerSeasonTotals
{
    public int PlayerId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string HeadShot { get; set; } = string.Empty;
    public POSITION Position { get; set; }
    public bool IsGoalie { get; set; }
    public int TeamId { get; set; }
    public string TeamAbbreviation { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public string TeamLogoUri { get; set; } = string.Empty;

    // Games with time on ice
    public int GamesPlayed { get; set; }
    public int TimeOnIceSeconds { get; set; }

    // Skaters
    public int Goals { get; set; }
    public int Assists { get; set; }
    public int Shots { get; set; }
    public int PlusMinus { get; set; }
    public int PenaltyMinutes { get; set; }
    public int PowerPlayGoals { get; set; }
    public int Hits { get; set; }
    public int BlockedShots { get; set; }

    // Goalies
    public int GamesStarted { get; set; }
    public int Saves { get; set; }
    public int GoalsAgainst { get; set; }
    // Games they played alone for their team without allowing a goal
    public int Shutouts { get; set; }
}
