using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.DbModels;

// Recent means within last few games ex. 5 games (override in appsettings)
public class DbGameCleaned
{
    [Key]
    public int GameId { get; set; }
    public double HomeWinRatio { get; set; }
    public double HomeRecentWinRatio { get; set; }
    public double HomeRecentGoalsAvg { get; set; }
    public double HomeRecentConcededGoalsAvg { get; set; }
    public double HomeRecentSogAvg { get; set; }
    public double HomeRecentPpgAvg { get; set; }
    public double HomeRecentHitsAvg { get; set; }
    public double HomeRecentPimAvg { get; set; }
    public double HomeRecentBlockedShotsAvg { get; set; }
    public double HomeRecentTakeawaysAvg { get; set; }
    public double HomeRecentGiveawaysAvg { get; set; }
    public double HomeGoalsAvg { get; set; }
    public double HomeGoalsAvgAtHome { get; set; }
    public double HomeRecentGoalsAvgAtHome { get; set; }
    public double HomeConcededGoalsAvg { get; set; }
    public double HomeConcededGoalsAvgAtHome { get; set; }
    public double HomeRecentConcededGoalsAvgAtHome { get; set; }
    public double HomeHoursSinceLastGame { get; set; }
    public double AwayWinRatio { get; set; }
    public double AwayRecentWinRatio { get; set; }
    public double AwayRecentGoalsAvg { get; set; }
    public double AwayRecentConcededGoalsAvg { get; set; }
    public double AwayRecentSogAvg { get; set; }
    public double AwayRecentPpgAvg { get; set; }
    public double AwayRecentHitsAvg { get; set; }
    public double AwayRecentPimAvg { get; set; }
    public double AwayRecentBlockedShotsAvg { get; set; }
    public double AwayRecentTakeawaysAvg { get; set; }
    public double AwayRecentGiveawaysAvg { get; set; }
    public double AwayGoalsAvg { get; set; }
    public double AwayGoalsAvgAtAway { get; set; }
    public double AwayRecentGoalsAvgAtAway { get; set; }
    public double AwayConcededGoalsAvg { get; set; }
    public double AwayConcededGoalsAvgAtAway { get; set; }
    public double AwayRecentConcededGoalsAvgAtAway { get; set; }
    public double HomeRosterOffenseValue { get; set; }
    public double HomeRosterDefenseValue { get; set; }
    public double HomeRosterGoalieValue { get; set; }
    public double AwayRosterOffenseValue { get; set; }
    public double AwayRosterDefenseValue { get; set; }
    public double AwayRosterGoalieValue { get; set; }
    public double HomeRecentRosterOffenseValue { get; set; }
    public double HomeRecentRosterDefenseValue { get; set; }
    public double HomeRecentRosterGoalieValue { get; set; }
    public double AwayRecentRosterOffenseValue { get; set; }
    public double AwayRecentRosterDefenseValue { get; set; }
    public double AwayRecentRosterGoalieValue { get; set; }
    public double AwayHoursSinceLastGame { get; set; }

    [ForeignKey(nameof(GameId))]
    public DbGameRaw? Game { get; set; } = new DbGameRaw();

    /// <summary>
    /// Clones a given game into this object
    /// </summary>
    /// <param name="game">Game to clone</param>
    public void Clone(DbGameCleaned gameCleaned)
    {
        GameId = gameCleaned.GameId;
        HomeWinRatio = gameCleaned.HomeWinRatio;
        HomeRecentWinRatio = gameCleaned.HomeRecentWinRatio;
        HomeRecentGoalsAvg = gameCleaned.HomeRecentGoalsAvg;
        HomeRecentConcededGoalsAvg = gameCleaned.HomeRecentConcededGoalsAvg;
        HomeRecentSogAvg = gameCleaned.HomeRecentSogAvg;
        HomeRecentPpgAvg = gameCleaned.HomeRecentPpgAvg;
        HomeRecentHitsAvg = gameCleaned.HomeRecentHitsAvg;
        HomeRecentPimAvg = gameCleaned.HomeRecentPimAvg;
        HomeRecentBlockedShotsAvg = gameCleaned.HomeRecentBlockedShotsAvg;
        HomeRecentTakeawaysAvg = gameCleaned.HomeRecentTakeawaysAvg;
        HomeRecentGiveawaysAvg = gameCleaned.HomeRecentGiveawaysAvg;
        HomeGoalsAvg = gameCleaned.HomeGoalsAvg;
        HomeGoalsAvgAtHome = gameCleaned.HomeGoalsAvgAtHome;
        HomeRecentGoalsAvgAtHome = gameCleaned.HomeRecentGoalsAvgAtHome;
        HomeConcededGoalsAvg = gameCleaned.HomeConcededGoalsAvg;
        HomeConcededGoalsAvgAtHome = gameCleaned.HomeConcededGoalsAvgAtHome;
        HomeRecentConcededGoalsAvgAtHome = gameCleaned.HomeRecentConcededGoalsAvgAtHome;
        HomeHoursSinceLastGame = gameCleaned.HomeHoursSinceLastGame;
        AwayWinRatio = gameCleaned.AwayWinRatio;
        AwayRecentWinRatio = gameCleaned.AwayRecentWinRatio;
        AwayRecentGoalsAvg = gameCleaned.AwayRecentGoalsAvg;
        AwayRecentConcededGoalsAvg = gameCleaned.AwayRecentConcededGoalsAvg;
        AwayRecentSogAvg = gameCleaned.AwayRecentSogAvg;
        AwayRecentPpgAvg = gameCleaned.AwayRecentPpgAvg;
        AwayRecentHitsAvg = gameCleaned.AwayRecentHitsAvg;
        AwayRecentPimAvg = gameCleaned.AwayRecentPimAvg;
        AwayRecentBlockedShotsAvg = gameCleaned.AwayRecentBlockedShotsAvg;
        AwayRecentTakeawaysAvg = gameCleaned.AwayRecentTakeawaysAvg;
        AwayRecentGiveawaysAvg = gameCleaned.AwayRecentGiveawaysAvg;
        AwayGoalsAvg = gameCleaned.AwayGoalsAvg;
        AwayGoalsAvgAtAway = gameCleaned.AwayGoalsAvgAtAway;
        AwayRecentGoalsAvgAtAway = gameCleaned.AwayRecentGoalsAvgAtAway;
        AwayConcededGoalsAvg = gameCleaned.AwayConcededGoalsAvg;
        AwayConcededGoalsAvgAtAway = gameCleaned.AwayConcededGoalsAvgAtAway;
        AwayRecentConcededGoalsAvgAtAway = gameCleaned.AwayRecentConcededGoalsAvgAtAway;
        HomeRosterOffenseValue = gameCleaned.HomeRosterOffenseValue;
        HomeRosterDefenseValue = gameCleaned.HomeRosterDefenseValue;
        HomeRosterGoalieValue = gameCleaned.HomeRosterGoalieValue;
        AwayRosterOffenseValue = gameCleaned.AwayRosterOffenseValue;
        AwayRosterDefenseValue = gameCleaned.AwayRosterDefenseValue;
        AwayRosterGoalieValue = gameCleaned.AwayRosterGoalieValue;
        HomeRecentRosterOffenseValue = gameCleaned.HomeRecentRosterOffenseValue;
        HomeRecentRosterDefenseValue = gameCleaned.HomeRecentRosterDefenseValue;
        HomeRecentRosterGoalieValue = gameCleaned.HomeRecentRosterGoalieValue;
        AwayRecentRosterOffenseValue = gameCleaned.AwayRecentRosterOffenseValue;
        AwayRecentRosterDefenseValue = gameCleaned.AwayRecentRosterDefenseValue;
        AwayRecentRosterGoalieValue = gameCleaned.AwayRecentRosterGoalieValue;
        AwayHoursSinceLastGame = gameCleaned.AwayHoursSinceLastGame;
        Game = gameCleaned.Game;
    }

    public bool IsEquivalentTo(DbGameCleaned? other)
    {
        if (other == null) return false;
        return GameId == other.GameId
            && HomeWinRatio == other.HomeWinRatio
            && HomeRecentWinRatio == other.HomeRecentWinRatio
            && HomeRecentGoalsAvg == other.HomeRecentGoalsAvg
            && HomeRecentConcededGoalsAvg == other.HomeRecentConcededGoalsAvg
            && HomeRecentSogAvg == other.HomeRecentSogAvg
            && HomeRecentPpgAvg == other.HomeRecentPpgAvg
            && HomeRecentHitsAvg == other.HomeRecentHitsAvg
            && HomeRecentPimAvg == other.HomeRecentPimAvg
            && HomeRecentBlockedShotsAvg == other.HomeRecentBlockedShotsAvg
            && HomeRecentTakeawaysAvg == other.HomeRecentTakeawaysAvg
            && HomeRecentGiveawaysAvg == other.HomeRecentGiveawaysAvg
            && HomeGoalsAvg == other.HomeGoalsAvg
            && HomeGoalsAvgAtHome == other.HomeGoalsAvgAtHome
            && HomeRecentGoalsAvgAtHome == other.HomeRecentGoalsAvgAtHome
            && HomeConcededGoalsAvg == other.HomeConcededGoalsAvg
            && HomeConcededGoalsAvgAtHome == other.HomeConcededGoalsAvgAtHome
            && HomeRecentConcededGoalsAvgAtHome == other.HomeRecentConcededGoalsAvgAtHome
            && HomeHoursSinceLastGame == other.HomeHoursSinceLastGame
            && AwayWinRatio == other.AwayWinRatio
            && AwayRecentWinRatio == other.AwayRecentWinRatio
            && AwayRecentGoalsAvg == other.AwayRecentGoalsAvg
            && AwayRecentConcededGoalsAvg == other.AwayRecentConcededGoalsAvg
            && AwayRecentSogAvg == other.AwayRecentSogAvg
            && AwayRecentPpgAvg == other.AwayRecentPpgAvg
            && AwayRecentHitsAvg == other.AwayRecentHitsAvg
            && AwayRecentPimAvg == other.AwayRecentPimAvg
            && AwayRecentBlockedShotsAvg == other.AwayRecentBlockedShotsAvg
            && AwayRecentTakeawaysAvg == other.AwayRecentTakeawaysAvg
            && AwayRecentGiveawaysAvg == other.AwayRecentGiveawaysAvg
            && AwayGoalsAvg == other.AwayGoalsAvg
            && AwayGoalsAvgAtAway == other.AwayGoalsAvgAtAway
            && AwayRecentGoalsAvgAtAway == other.AwayRecentGoalsAvgAtAway
            && AwayConcededGoalsAvg == other.AwayConcededGoalsAvg
            && AwayConcededGoalsAvgAtAway == other.AwayConcededGoalsAvgAtAway
            && AwayRecentConcededGoalsAvgAtAway == other.AwayRecentConcededGoalsAvgAtAway
            && HomeRosterOffenseValue == other.HomeRosterOffenseValue
            && HomeRosterDefenseValue == other.HomeRosterDefenseValue
            && HomeRosterGoalieValue == other.HomeRosterGoalieValue
            && AwayRosterOffenseValue == other.AwayRosterOffenseValue
            && AwayRosterDefenseValue == other.AwayRosterDefenseValue
            && AwayRosterGoalieValue == other.AwayRosterGoalieValue
            && HomeRecentRosterOffenseValue == other.HomeRecentRosterOffenseValue
            && HomeRecentRosterDefenseValue == other.HomeRecentRosterDefenseValue
            && HomeRecentRosterGoalieValue == other.HomeRecentRosterGoalieValue
            && AwayRecentRosterOffenseValue == other.AwayRecentRosterOffenseValue
            && AwayRecentRosterDefenseValue == other.AwayRecentRosterDefenseValue
            && AwayRecentRosterGoalieValue == other.AwayRecentRosterGoalieValue
            && AwayHoursSinceLastGame == other.AwayHoursSinceLastGame;
    }
}