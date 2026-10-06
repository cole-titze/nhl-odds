namespace Entities.ViewModels;

// Backtest of one strategy/threshold against one book, broken out by season
public class StrategySeasonResultsVM
{
    // "DraftKings", "Kalshi", or "best" (the pinned book with the largest edge)
    public string Book { get; set; } = string.Empty;
    public string BetType { get; set; } = string.Empty;
    public string StrategyType { get; set; } = string.Empty;
    public double Threshold { get; set; }
    public List<SeasonResultVM> Seasons { get; set; } = new();
}

public class SeasonResultVM
{
    public int Season { get; set; }
    public int Bets { get; set; }
    public double Roi { get; set; }
}
