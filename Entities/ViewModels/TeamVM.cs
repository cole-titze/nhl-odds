namespace Entities.ViewModels;

public class TeamVM
{
    public int Id { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public string LogoUri { get; set; } = string.Empty;
    public double ModelLogLoss { get; set; }
    public int TotalGameCount { get; set; }
    public int SeasonWins { get; set; }
    public int SeasonLosses { get; set; }
    public int SeasonOvertimeLosses { get; set; }
    public int TotalModelAccurateGameCount { get; set; }
    public double DraftKingsLogLoss { get; set; }
    public int DraftKingsAccurateGameCount { get; set; }
    public int DraftKingsGameCount { get; set; }
    public IEnumerable<GameOddsVM> GameOddsVM { get; set; } = new List<GameOddsVM>();
}