namespace Entities.Models.Web;

/// <summary>
/// The players in a game: its box score once played (Lines), else both teams' current rosters (Roster).
/// </summary>
public class GamePlayers
{
    public Game Game { get; set; } = new Game();
    public List<PlayerGameLine> Lines { get; set; } = new();
    public List<RosterPlayer> Roster { get; set; } = new();
}
