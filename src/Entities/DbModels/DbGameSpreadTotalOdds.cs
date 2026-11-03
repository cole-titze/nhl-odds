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
    // NULL is allowed in CreateTables.sql; the predictor always writes it, but a NULL must not break game queries
    public string? Notes { get; set; }
    [ForeignKey(nameof(GameId))]
    public DbGameRaw? Game { get; set; }
}