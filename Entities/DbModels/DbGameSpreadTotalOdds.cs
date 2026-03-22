using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.DbModels;

[Table("GameSpreadTotalOdds")]
public class DbGameSpreadTotalOdds
{
    public int GameId { get; set; }
    public int ModelId { get; set; }
    public DateTime RunDateUTC { get; set; }
    public double PredictedValue { get; set; }
    public double ResidualStd { get; set; }
    public double? Line { get; set; }
    public double? CoverProbability { get; set; }
    public string Notes { get; set; } = string.Empty;
    [ForeignKey(nameof(GameId))]
    public DbGameRaw? Game { get; set; }
}
