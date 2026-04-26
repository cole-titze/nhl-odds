namespace Entities.ViewModels;

public class SeasonTotalsVM
{
    public double ModelLogLoss { get; set; }
    public int TotalGameCount { get; set; }
    public int TotalModelAccurateGameCount { get; set; }
    public double DraftKingsLogLoss { get; set; }
    public int DraftKingsAccurateGameCount { get; set; }
    public int DraftKingsGameCount { get; set; }
}