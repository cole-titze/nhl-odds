using Entities.Types;

namespace Entities.Models.Web;

/// <summary>The team and position of a player's latest game, and their current team (see PlayerProfile.CurrentTeam).</summary>
public class PlayerLatestTeam
{
    public int PlayerId { get; set; }
    public POSITION Position { get; set; }
    public int TeamId { get; set; }
    public string TeamAbbreviation { get; set; } = string.Empty;
    public int? CurrentTeamId { get; set; }
    public string? CurrentTeamAbbreviation { get; set; }
}
