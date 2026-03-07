using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.DbModels;

public class DbGameOdds
{
    public int GameId { get; set; }
    public int ModelName { get; set; }
    public DateTime RunDateUTC { get; set; }
    public double HomeOdds { get; set; }
    public double AwayOdds { get; set; }
    public double LogLoss { get; set; }
    public string Notes { get; set; } = string.Empty;
    [ForeignKey(nameof(GameId))]
    public DbGameRaw? Game { get; set; }
    public void Clone(DbGameOdds gameOdds)
    {
        GameId = gameOdds.GameId;
        ModelName = gameOdds.ModelName;
        RunDateUTC = gameOdds.RunDateUTC;
        HomeOdds = gameOdds.HomeOdds;
        AwayOdds = gameOdds.AwayOdds;
        LogLoss = gameOdds.LogLoss;
        Notes = gameOdds.Notes;
        Game = gameOdds.Game;
    }
}