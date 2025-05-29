using System.ComponentModel.DataAnnotations.Schema;
using Entities.Models;

namespace Entities.DbModels
{
    public class DbGameSkaterStats : IDbGamePlayerStats
    {
        public int gameId { get; set; } = -1;
        public int playerId { get; set; } = -1;
        public int teamId { get; set; }
        public int goals { get; set; }
        public int assists { get; set; }
        public int plusMinus { get; set; }
        public int penaltyMinutes { get; set; }
        public int hits { get; set; }
        public int powerPlayGoals { get; set; }
        public int shotsOnGoal { get; set; }
        public int faceOffWinningPctg { get; set; }
        public int blockedShots { get; set; }
        public int giveaways { get; set; }
        public int takeaways { get; set; }
        public int timeOnIceSeconds { get; set; }
        public POSITION position { get; set; } = POSITION.LeftWing;

        [ForeignKey("playerId")]
        public DbPlayer player { get; set; } = new DbPlayer();
        [ForeignKey("gameId")]
        public DbGameRaw game { get; set; } = new DbGameRaw();
        [ForeignKey("teamId")]
        public DbTeam team { get; set; } = new DbTeam();
        public void Clone(IDbGamePlayerStats gamePlayerStats)
        {
            if (gamePlayerStats is DbGameSkaterStats gameSkaterStats)
            {
                gameId = gameSkaterStats.gameId;
                playerId = gameSkaterStats.playerId;
                goals = gameSkaterStats.goals;
                assists = gameSkaterStats.assists;
                plusMinus = gameSkaterStats.plusMinus;
                penaltyMinutes = gameSkaterStats.penaltyMinutes;
                hits = gameSkaterStats.hits;
                powerPlayGoals = gameSkaterStats.powerPlayGoals;
                shotsOnGoal = gameSkaterStats.shotsOnGoal;
                faceOffWinningPctg = gameSkaterStats.faceOffWinningPctg;
                blockedShots = gameSkaterStats.blockedShots;
                giveaways = gameSkaterStats.giveaways;
                takeaways = gameSkaterStats.takeaways;
                timeOnIceSeconds = gameSkaterStats.timeOnIceSeconds;
                player = gameSkaterStats.player;
                game = gameSkaterStats.game;
                team = gameSkaterStats.team;
            }
            else
            {
                throw new InvalidOperationException("Invalid type passed to CloneFrom.");
            }
        }
        public bool IsValid()
        {
            if (gameId == -1 && playerId == -1)
                return false;
            return true;
        }
    }
}