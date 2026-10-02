namespace Entities.ViewModels;

public class BestStrategyVM
{
    // "moneyline" | "spread" | "overUnder" — matches the frontend BetTypeCategory
    public string BetType { get; set; } = string.Empty;
    // Matches the frontend StrategyType (e.g. "underdog", "totalValue")
    public string StrategyType { get; set; } = string.Empty;
    public double Threshold { get; set; }
    public int Bets { get; set; }
    public int Wins { get; set; }
    public double Roi { get; set; }
    public int FirstSeason { get; set; }
    public int LastSeason { get; set; }
}
