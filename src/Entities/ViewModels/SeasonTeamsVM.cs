namespace Entities.ViewModels;

/// <summary>
/// The Teams page's data: each team's season stats and every game once, with only the fields the
/// page reads and only DraftKings' and Kalshi's odds. GetAllTeams lists each game twice (once per team) with every field, which late in a
/// season is ~8MB to parse on the frontend server for every page load.
/// </summary>
public class SeasonTeamsVM
{
    public IEnumerable<TeamSummaryVM> Teams { get; set; } = new List<TeamSummaryVM>();
    public SeasonTotalsVM SeasonTotals { get; set; } = new SeasonTotalsVM();
    public IEnumerable<SeasonTeamGameVM> Games { get; set; } = new List<SeasonTeamGameVM>();
    // Books with odds this season besides the ones in Games; the page loads one when it's picked
    public IEnumerable<string> OtherBookmakers { get; set; } = new List<string>();
}