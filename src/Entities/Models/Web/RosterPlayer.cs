using Entities.Types;

namespace Entities.Models.Web;

/// <summary>A player on a team's roster: his position and number in his latest game, and his games for the team that season.</summary>
public class RosterPlayer
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string HeadShot { get; set; } = string.Empty;
    public int TeamId { get; set; }
    public POSITION Position { get; set; }
    public short? SweaterNumber { get; set; }
    public int GamesPlayed { get; set; }
}
