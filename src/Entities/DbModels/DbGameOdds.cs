using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.DbModels;

public class DbGameOdds
{
    public int GameId { get; set; }
    public int ModelId { get; set; }
    public DateTime RunDateUTC { get; set; }
    public double HomeOdds { get; set; }
    public double AwayOdds { get; set; }
    public double LogLoss { get; set; }
    // NULL is allowed in CreateTables.sql; the predictor always writes it, but a NULL must not break game queries
    public string? Notes { get; set; }
    [ForeignKey(nameof(GameId))]
    public DbGameRaw? Game { get; set; }
    public void Clone(DbGameOdds gameOdds)
    {
        GameId = gameOdds.GameId;
        ModelId = gameOdds.ModelId;
        RunDateUTC = gameOdds.RunDateUTC;
        HomeOdds = gameOdds.HomeOdds;
        AwayOdds = gameOdds.AwayOdds;
        LogLoss = gameOdds.LogLoss;
        Notes = gameOdds.Notes;
    }
}