namespace Entities.ViewModels;

public class PredictionFactorsVM
{
    public int GameId { get; set; }
    public DateTime RunDateUTC { get; set; }
    public double BaseHomeOdds { get; set; }
    public IEnumerable<PredictionFactorVM> Factors { get; set; } = [];
}

public class PredictionFactorVM
{
    public string Factor { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public double Contribution { get; set; }
}
