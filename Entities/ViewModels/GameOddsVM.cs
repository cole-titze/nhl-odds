using Entities.Types;

namespace Entities.ViewModels;

public class GameOddsVM
{
    public int id { get; set; }
    public DateTime gameDate { get; set; }
    public MatchupTeamVM? homeTeam { get; set; }
    public MatchupTeamVM? awayTeam { get; set; }
    public Winner winner { get; set; }
    public bool hasBeenPlayed { get; set; }
}
