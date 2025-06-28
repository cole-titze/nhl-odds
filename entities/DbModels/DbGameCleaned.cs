using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.DbModels
{
    // Recent means within last few games ex. 5 games (override in appsettings)
    public class DbGameCleaned
    {
        [Key]
        public int gameId { get; set; }
        public double homeWinRatio { get; set; }
        public double homeRecentWinRatio { get; set; }
        public double homeRecentGoalsAvg { get; set; }
        public double homeRecentConcededGoalsAvg { get; set; }
        public double homeRecentSogAvg { get; set; }
        public double homeRecentPpgAvg { get; set; }
        public double homeRecentHitsAvg { get; set; }
        public double homeRecentPimAvg { get; set; }
        public double homeRecentBlockedShotsAvg { get; set; }
        public double homeRecentTakeawaysAvg { get; set; }
        public double homeRecentGiveawaysAvg { get; set; }
        public double homeGoalsAvg { get; set; }
        public double homeGoalsAvgAtHome { get; set; }
        public double homeRecentGoalsAvgAtHome { get; set; }
        public double homeConcededGoalsAvg { get; set; }
        public double homeConcededGoalsAvgAtHome { get; set; }
        public double homeRecentConcededGoalsAvgAtHome { get; set; }
        public double homeHoursSinceLastGame { get; set; }
        public double awayWinRatio { get; set; }
        public double awayRecentWinRatio { get; set; }
        public double awayRecentGoalsAvg { get; set; }
        public double awayRecentConcededGoalsAvg { get; set; }
        public double awayRecentSogAvg { get; set; }
        public double awayRecentPpgAvg { get; set; }
        public double awayRecentHitsAvg { get; set; }
        public double awayRecentPimAvg { get; set; }
        public double awayRecentBlockedShotsAvg { get; set; }
        public double awayRecentTakeawaysAvg { get; set; }
        public double awayRecentGiveawaysAvg { get; set; }
        public double awayGoalsAvg { get; set; }
        public double awayGoalsAvgAtAway { get; set; }
        public double awayRecentGoalsAvgAtAway { get; set; }
        public double awayConcededGoalsAvg { get; set; }
        public double awayConcededGoalsAvgAtAway { get; set; }
        public double awayRecentConcededGoalsAvgAtAway { get; set; }
        public double homeRosterOffenseValue { get; set; }
        public double homeRosterDefenseValue { get; set; }
        public double homeRosterGoalieValue { get; set; }
        public double awayRosterOffenseValue { get; set; }
        public double awayRosterDefenseValue { get; set; }
        public double awayRosterGoalieValue { get; set; }
        public double awayHoursSinceLastGame { get; set; }

        [ForeignKey(nameof(gameId))]
        public DbGameRaw? game { get; set; } = new DbGameRaw();

        /// <summary>
        /// Clones a given game into this object
        /// </summary>
        /// <param name="game">Game to clone</param>
        public void Clone(DbGameCleaned gameCleaned)
        {
            gameId = gameCleaned.gameId;
            homeWinRatio = gameCleaned.homeWinRatio;
            homeRecentWinRatio = gameCleaned.homeRecentWinRatio;
            homeRecentGoalsAvg = gameCleaned.homeRecentGoalsAvg;
            homeRecentConcededGoalsAvg = gameCleaned.homeRecentConcededGoalsAvg;
            homeRecentSogAvg = gameCleaned.homeRecentSogAvg;
            homeRecentPpgAvg = gameCleaned.homeRecentPpgAvg;
            homeRecentHitsAvg = gameCleaned.homeRecentHitsAvg;
            homeRecentPimAvg = gameCleaned.homeRecentPimAvg;
            homeRecentBlockedShotsAvg = gameCleaned.homeRecentBlockedShotsAvg;
            homeRecentTakeawaysAvg = gameCleaned.homeRecentTakeawaysAvg;
            homeRecentGiveawaysAvg = gameCleaned.homeRecentGiveawaysAvg;
            homeGoalsAvg = gameCleaned.homeGoalsAvg;
            homeGoalsAvgAtHome = gameCleaned.homeGoalsAvgAtHome;
            homeRecentGoalsAvgAtHome = gameCleaned.homeRecentGoalsAvgAtHome;
            homeConcededGoalsAvg = gameCleaned.homeConcededGoalsAvg;
            homeConcededGoalsAvgAtHome = gameCleaned.homeConcededGoalsAvgAtHome;
            homeRecentConcededGoalsAvgAtHome = gameCleaned.homeRecentConcededGoalsAvgAtHome;
            homeHoursSinceLastGame = gameCleaned.homeHoursSinceLastGame;
            awayWinRatio = gameCleaned.awayWinRatio;
            awayRecentWinRatio = gameCleaned.awayRecentWinRatio;
            awayRecentGoalsAvg = gameCleaned.awayRecentGoalsAvg;
            awayRecentConcededGoalsAvg = gameCleaned.awayRecentConcededGoalsAvg;
            awayRecentSogAvg = gameCleaned.awayRecentSogAvg;
            awayRecentPpgAvg = gameCleaned.awayRecentPpgAvg;
            awayRecentHitsAvg = gameCleaned.awayRecentHitsAvg;
            awayRecentPimAvg = gameCleaned.awayRecentPimAvg;
            awayRecentBlockedShotsAvg = gameCleaned.awayRecentBlockedShotsAvg;
            awayRecentTakeawaysAvg = gameCleaned.awayRecentTakeawaysAvg;
            awayRecentGiveawaysAvg = gameCleaned.awayRecentGiveawaysAvg;
            awayGoalsAvg = gameCleaned.awayGoalsAvg;
            awayGoalsAvgAtAway = gameCleaned.awayGoalsAvgAtAway;
            awayRecentGoalsAvgAtAway = gameCleaned.awayRecentGoalsAvgAtAway;
            awayConcededGoalsAvg = gameCleaned.awayConcededGoalsAvg;
            awayConcededGoalsAvgAtAway = gameCleaned.awayConcededGoalsAvgAtAway;
            awayRecentConcededGoalsAvgAtAway = gameCleaned.awayRecentConcededGoalsAvgAtAway;
            homeRosterOffenseValue = gameCleaned.homeRosterOffenseValue;
            homeRosterDefenseValue = gameCleaned.homeRosterDefenseValue;
            homeRosterGoalieValue = gameCleaned.homeRosterGoalieValue;
            awayRosterOffenseValue = gameCleaned.awayRosterOffenseValue;
            awayRosterDefenseValue = gameCleaned.awayRosterDefenseValue;
            awayRosterGoalieValue = gameCleaned.awayRosterGoalieValue;
            awayHoursSinceLastGame = gameCleaned.awayHoursSinceLastGame;
            game = gameCleaned.game;
        }
    }
}