using Entities.Types;

namespace Entities.Models.Web;

/// <summary>One player's box score line for one game: skater or goalie stats, by Position.</summary>
public class PlayerGameLine
{
    public int PlayerId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public short? SweaterNumber { get; set; }
    public int TeamId { get; set; }
    public POSITION Position { get; set; }
    public Game Game { get; set; } = new Game();
    public int TimeOnIceSeconds { get; set; }
    // Skaters
    public int Goals { get; set; }
    public int Assists { get; set; }
    public int ShotsOnGoal { get; set; }
    public int PlusMinus { get; set; }
    // Goalies
    public int ShotsAgainst { get; set; }
    public int Saves { get; set; }
    public int GoalsAgainst { get; set; }
    public bool IsStarter { get; set; }

    public bool IsGoalie => Position == POSITION.Goalie;
}
