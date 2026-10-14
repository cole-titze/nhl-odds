using Entities.Types;

namespace Entities.ViewModels;

public class SeasonTeamGameVM
{
    public int Id { get; set; }
    public DateTime GameDate { get; set; }
    public bool HasBeenPlayed { get; set; }
    public double? LogLoss { get; set; }
    public Winner Winner { get; set; }
    public int HomeTeamId { get; set; }
    public int AwayTeamId { get; set; }
    public IEnumerable<SeasonTeamGameOddsVM> BookmakerOdds { get; set; } = new List<SeasonTeamGameOddsVM>();
}