using Entities.Types;

namespace Entities.Models
{
    public class GameGoalieStats : IGamePlayerStats
    {
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
    }
}

