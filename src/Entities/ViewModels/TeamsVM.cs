namespace Entities.ViewModels;

public class TeamsVM
{
    public IEnumerable<TeamVM> Teams { get; set; } = new List<TeamVM>();
    public SeasonTotalsVM SeasonTotals { get; set; } = new SeasonTotalsVM();
}