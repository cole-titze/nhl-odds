namespace Entities.Models
{
    public class GameGoalieStats : IGamePlayerStats
    {
        public int playerId { get; set; }
        public int gameId { get; set; }
        public int teamId { get; set; }
        public int evenStrengthShotsSaved { get; set; }
        public int powerPlayShotsSaved { get; set; }
        public int evenStrengthGoalsAllowed { get; set; }
        public int powerPlayGoalsAllowed { get; set; }
        public int timeOnIceSeconds { get; set; }
        public bool isStarter { get; set; }
        public POSITION position { get; set; } = POSITION.Goalie;
        public int SeasonStartYear()
        {
            return gameId / 1000000;
        }

        // Goalie Game Score = ( (-0.75 * GA) + (0.1 * SV) ) / GP
        // public double GetPlayerValue()
        // {
        //     if (gamesStarted < 5)
        //         return 0;

        //     var value = ((-.75 * goalsAgainst) + (.1 * saves)) / gamesStarted;

        //     return Math.Max(value * 60, 0);
        // }
    }
}

