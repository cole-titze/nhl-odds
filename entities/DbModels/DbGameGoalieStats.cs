using System.ComponentModel.DataAnnotations.Schema;
using Entities.Models;

namespace Entities.DbModels
{
    public class DbGameGoalieStats : IDbGamePlayerStats
    {
        public int gameId { get; set; }
        public int playerId { get; set; }
        public int teamId { get; set; }
        public int evenStrengthShotsSaved { get; set; }
        public int powerPlayShotsSaved { get; set; }
        public int shortHandedShotsSaved { get; set; }
        public int evenStrengthGoalsAllowed { get; set; }
        public int powerPlayGoalsAllowed { get; set; }
        public int shortHandedGoalsAllowed { get; set; }
        public int timeOnIceSeconds { get; set; }
        public bool isStarter { get; set; }
        public POSITION position { get; set; } = POSITION.Goalie;
        [ForeignKey("playerId")]
        public DbPlayer? player { get; set; }
        [ForeignKey("gameId")]
        public DbGameRaw? game { get; set; }
        [ForeignKey("teamId")]
        public DbTeam? team { get; set; }
        public void Clone(IDbGamePlayerStats gamePlayerStats)
        {
            if (gamePlayerStats is DbGameGoalieStats goalieStats)
            {
                gameId = goalieStats.gameId;
                teamId = goalieStats.teamId;
                playerId = goalieStats.playerId;
                evenStrengthShotsSaved = goalieStats.evenStrengthShotsSaved;
                powerPlayShotsSaved = goalieStats.powerPlayShotsSaved;
                evenStrengthGoalsAllowed = goalieStats.evenStrengthGoalsAllowed;
                powerPlayGoalsAllowed = goalieStats.powerPlayGoalsAllowed;
                timeOnIceSeconds = goalieStats.timeOnIceSeconds;
                isStarter = goalieStats.isStarter;
                position = goalieStats.position;
                player = goalieStats.player;
                game = goalieStats.game;
                team = goalieStats.team;
            }
            else
            {
                throw new InvalidOperationException("Invalid type passed to CloneFrom.");
            }
        }
    }
}
