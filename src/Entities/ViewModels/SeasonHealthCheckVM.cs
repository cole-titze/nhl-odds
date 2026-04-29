namespace Entities.ViewModels;

public class SeasonHealthCheckVM
{
    public int SeasonStartYear { get; set; }
    public int TotalGames { get; set; }
    public int PlayedGames { get; set; }
    public int MissingPredictions { get; set; }
    public int MissingBookmakerOdds { get; set; }
    public int MissingGameCleaned { get; set; }
    public int MissingOddsFetchDays { get; set; }
    public int LiveBookmakerOdds { get; set; }
    public int MissingKalshiOdds { get; set; }
    public int ErrorCount { get; set; }
}