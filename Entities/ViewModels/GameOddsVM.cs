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
    public int ModelId { get; set; }
    public List<BookmakerOddsVM> BookmakerOdds { get; set; } = new();
    public double? PredictedSpread { get; set; }
    public double? SpreadCoverProb { get; set; }
    public double? PredictedTotal { get; set; }
    public double? TotalOverProb { get; set; }
}
