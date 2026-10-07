namespace Entities.ViewModels;

public class TeamsSummaryVM
{
    public IEnumerable<TeamSummaryVM> Teams { get; set; } = new List<TeamSummaryVM>();
    public SeasonTotalsVM SeasonTotals { get; set; } = new SeasonTotalsVM();
}
