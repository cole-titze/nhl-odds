using Entities.Types;

namespace Entities.Models.Web;

/// <summary>The team and position of a player's latest game.</summary>
public class PlayerLatestTeam
{
    public int PlayerId { get; set; }
    public POSITION Position { get; set; }
    public int TeamId { get; set; }
    public string TeamAbbreviation { get; set; } = string.Empty;
}
