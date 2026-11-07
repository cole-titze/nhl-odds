using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.DbModels;

/// <summary>One feature group's effect on a GameOdds prediction (game_predictor/explain, #155).</summary>
[Table("GamePredictionFactor")]
public class DbGamePredictionFactor
{
    public int GameId { get; set; }
    public int ModelId { get; set; }
    public DateTime RunDateUTC { get; set; }
    /// <summary>1 = the largest effect for this prediction</summary>
    public int Rank { get; set; }
    public string Factor { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    /// <summary>Change in the home team's win probability; positive favors home</summary>
    public double Contribution { get; set; }
    /// <summary>Average prediction over the background games the contributions start from</summary>
    public double BaseHomeOdds { get; set; }
}
