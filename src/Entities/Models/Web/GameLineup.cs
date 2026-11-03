namespace Entities.Models.Web;

/// <summary>
/// One game's projected lineup from NHL.com's lineup projections article: the newest version of its section
/// saved before puck drop.
/// </summary>
public class GameLineup
{
    public int GameId { get; set; }
    /// <summary>When this version of the game's section was first seen</summary>
    public DateTime UpdatedUTC { get; set; }
    /// <summary>How many versions of the section were seen before puck drop (more than one: it changed)</summary>
    public int VersionCount { get; set; }
    public string? StatusReport { get; set; }
    public List<GameLineupPlayer> Players { get; set; } = [];
}

/// <param name="Side">"Away" or "Home"</param>
/// <param name="Group">F, D, G, Scratched, Injured or Suspended</param>
/// <param name="LineNumber">Line, pair or goalie order (1 is listed first); list order for the other groups</param>
/// <param name="Slot">Place on the line: forwards 1-3 (left wing, center, right wing), defense 1-2</param>
public record GameLineupPlayer(string Side, string Group, int LineNumber, int Slot, string Name, int? PlayerId, string? Note);
