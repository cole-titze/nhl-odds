using Entities.Types;

namespace Entities.ViewModels;

public class GameOddsVM
{
    public int Id { get; set; }
    public DateTime GameDate { get; set; }
    public MatchupTeamVM? HomeTeam { get; set; }
    public MatchupTeamVM? AwayTeam { get; set; }
    public Winner Winner { get; set; }
    public bool HasBeenPlayed { get; set; }
    public double LogLoss { get; set; }
    public int ModelName { get; set; }
}
