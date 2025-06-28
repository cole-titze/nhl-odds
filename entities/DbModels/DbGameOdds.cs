using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.DbModels
{
    public class DbGameOdds
    {
        public int gameId { get; set; }
        public string modelName { get; set; } = string.Empty;
        public DateTime runDateUTC { get; set; }
        public double homeOdds { get; set; }
        public double awayOdds { get; set; }
        public double logLoss { get; set; }
        public string notes { get; set; } = string.Empty;
        [ForeignKey(nameof(gameId))]
        public DbGameRaw? game { get; set; }
        public void Clone(DbGameOdds gameOdds)
        {
            gameId = gameOdds.gameId;
            modelName = gameOdds.modelName;
            runDateUTC = gameOdds.runDateUTC;
            homeOdds = gameOdds.homeOdds;
            awayOdds = gameOdds.awayOdds;
            logLoss = gameOdds.logLoss;
            notes = gameOdds.notes;
            game = gameOdds.game;
        }
    }
}