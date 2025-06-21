using Entities.Types;

namespace Entities.Models
{
    public class GameSkaterStats : IGamePlayerStats
    {
        public int playerId { get; set; }
        public int gameId { get; set; }
        public int teamId { get; set; }
        public int goals { get; set; }
        public int assists { get; set; }
        public int shotsOnGoal { get; set; }
        public int blockedShots { get; set; }
        public int penaltyMinutes { get; set; }
        public int powerPlayGoals { get; set; }
        public int plusMinus { get; set; }
        public double faceOffWinningPctg { get; set; }
        public int hits { get; set; }
        public int giveaways { get; set; }
        public int takeaways { get; set; }
        public int timeOnIceSeconds { get; set; }
        // Default to stop null warning left-wing has no special rules
        public POSITION position { get; set; } = POSITION.LeftWing;
    }
}
