namespace Entities.ViewModels;

/// <summary>
/// The players in a game, by team. Once it's played these are the players who dressed, with their lines
/// (HasStats); before, each team's current roster without stats.
/// </summary>
public class GamePlayersVM
{
    public int GameId { get; set; }
    public bool HasStats { get; set; }
    public IEnumerable<GamePlayerVM> Home { get; set; } = new List<GamePlayerVM>();
    public IEnumerable<GamePlayerVM> Away { get; set; } = new List<GamePlayerVM>();
}
