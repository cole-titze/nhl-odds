namespace Entities.Models.Web;

/// <summary>Why the model predicted a game the way it did (#155): each feature group's effect on the
/// home team's win probability, largest first. BaseHomeOdds plus every contribution is the prediction.</summary>
public class PredictionFactors
{
    public int GameId { get; set; }
    public DateTime RunDateUTC { get; set; }
    public double BaseHomeOdds { get; set; }
    public List<PredictionFactor> Factors { get; set; } = [];
}

public class PredictionFactor
{
    public string Factor { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    /// <summary>Change in the home team's win probability; positive favors home</summary>
    public double Contribution { get; set; }
}
