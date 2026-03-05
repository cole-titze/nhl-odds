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
    public double HomeIsBackToBack { get; set; }
    public double AwayIsBackToBack { get; set; }
    public double RestAdvantage { get; set; }
    public double HomeRecentShotAttemptsAvg { get; set; }
    public double AwayRecentShotAttemptsAvg { get; set; }
    public double HomeGoalDiffAvg { get; set; }
    public double AwayGoalDiffAvg { get; set; }
    public double HomeRecentGoalDiffAvg { get; set; }
    public double AwayRecentGoalDiffAvg { get; set; }
    public double HomeStreak { get; set; }
    public double AwayStreak { get; set; }
    public double HomeWinRatioAtHome { get; set; }
    public double AwayWinRatioAtAway { get; set; }
    public double HeadToHeadWinRatio { get; set; }
    public double HomeSavePct { get; set; }
    public double AwaySavePct { get; set; }
    public double HomeRecentSavePct { get; set; }
    public double AwayRecentSavePct { get; set; }
    public double HomeSogAvg { get; set; }
    public double AwaySogAvg { get; set; }
    public double HomePpgAvg { get; set; }
    public double AwayPpgAvg { get; set; }
    public double HomeHitsAvg { get; set; }
    public double AwayHitsAvg { get; set; }
    public double HomePimAvg { get; set; }
    public double AwayPimAvg { get; set; }
    public double HomeBlockedShotsAvg { get; set; }
    public double AwayBlockedShotsAvg { get; set; }
    public double HomeTakeawaysAvg { get; set; }
    public double AwayTakeawaysAvg { get; set; }
    public double HomeGiveawaysAvg { get; set; }
    public double AwayGiveawaysAvg { get; set; }
    public double HomeFaceOffWinPctAvg { get; set; }
    public double AwayFaceOffWinPctAvg { get; set; }
    public double HomeRecentFaceOffWinPctAvg { get; set; }
    public double AwayRecentFaceOffWinPctAvg { get; set; }
    public double HomeOvertimeRatio { get; set; }
    public double AwayOvertimeRatio { get; set; }
    public double HomeRecentOvertimeRatio { get; set; }
    public double AwayRecentOvertimeRatio { get; set; }
    public double HomeRegulationWinRatio { get; set; }
    public double AwayRegulationWinRatio { get; set; }
    public double HomeRecentRegulationWinRatio { get; set; }
    public double AwayRecentRegulationWinRatio { get; set; }
    public double HomePpEfficiency { get; set; }
    public double AwayPpEfficiency { get; set; }
    public double HomeRecentPpEfficiency { get; set; }
    public double AwayRecentPpEfficiency { get; set; }
    public double HomePkEfficiency { get; set; }
    public double AwayPkEfficiency { get; set; }
    public double HomeRecentPkEfficiency { get; set; }
    public double AwayRecentPkEfficiency { get; set; }
    public double HomeGoalsPerGamePeriod1 { get; set; }
    public double AwayGoalsPerGamePeriod1 { get; set; }
    public double HomeGoalsPerGamePeriod2 { get; set; }
    public double AwayGoalsPerGamePeriod2 { get; set; }
    public double HomeGoalsPerGamePeriod3 { get; set; }
    public double AwayGoalsPerGamePeriod3 { get; set; }
    public double HomeRecentGoalsPerGamePeriod1 { get; set; }
    public double AwayRecentGoalsPerGamePeriod1 { get; set; }
    public double HomeRecentGoalsPerGamePeriod2 { get; set; }
    public double AwayRecentGoalsPerGamePeriod2 { get; set; }
    public double HomeRecentGoalsPerGamePeriod3 { get; set; }
    public double AwayRecentGoalsPerGamePeriod3 { get; set; }
    public double HomeOffensiveZoneFaceoffWinPct { get; set; }
    public double AwayOffensiveZoneFaceoffWinPct { get; set; }
    public double HomeRecentOffensiveZoneFaceoffWinPct { get; set; }
    public double AwayRecentOffensiveZoneFaceoffWinPct { get; set; }
    public double HomePenaltyDifferentialAvg { get; set; }
    public double AwayPenaltyDifferentialAvg { get; set; }
    public double HomeRecentPenaltyDifferentialAvg { get; set; }
    public double AwayRecentPenaltyDifferentialAvg { get; set; }
    public double HomeShootingPct { get; set; }
    public double AwayShootingPct { get; set; }
    public double HomeRecentShootingPct { get; set; }
    public double AwayRecentShootingPct { get; set; }
    public double HomeCorsiPct { get; set; }
    public double AwayCorsiPct { get; set; }
    public double HomeRecentCorsiPct { get; set; }
    public double AwayRecentCorsiPct { get; set; }
    public double HomeStrengthOfSchedule { get; set; }
    public double AwayStrengthOfSchedule { get; set; }
    public double HomeRecentStrengthOfSchedule { get; set; }
    public double AwayRecentStrengthOfSchedule { get; set; }

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
        HomeIsBackToBack = gameCleaned.HomeIsBackToBack;
        AwayIsBackToBack = gameCleaned.AwayIsBackToBack;
        RestAdvantage = gameCleaned.RestAdvantage;
        HomeRecentShotAttemptsAvg = gameCleaned.HomeRecentShotAttemptsAvg;
        AwayRecentShotAttemptsAvg = gameCleaned.AwayRecentShotAttemptsAvg;
        HomeGoalDiffAvg = gameCleaned.HomeGoalDiffAvg;
        AwayGoalDiffAvg = gameCleaned.AwayGoalDiffAvg;
        HomeRecentGoalDiffAvg = gameCleaned.HomeRecentGoalDiffAvg;
        AwayRecentGoalDiffAvg = gameCleaned.AwayRecentGoalDiffAvg;
        HomeStreak = gameCleaned.HomeStreak;
        AwayStreak = gameCleaned.AwayStreak;
        HomeWinRatioAtHome = gameCleaned.HomeWinRatioAtHome;
        AwayWinRatioAtAway = gameCleaned.AwayWinRatioAtAway;
        HeadToHeadWinRatio = gameCleaned.HeadToHeadWinRatio;
        HomeSavePct = gameCleaned.HomeSavePct;
        AwaySavePct = gameCleaned.AwaySavePct;
        HomeRecentSavePct = gameCleaned.HomeRecentSavePct;
        AwayRecentSavePct = gameCleaned.AwayRecentSavePct;
        HomeSogAvg = gameCleaned.HomeSogAvg;
        AwaySogAvg = gameCleaned.AwaySogAvg;
        HomePpgAvg = gameCleaned.HomePpgAvg;
        AwayPpgAvg = gameCleaned.AwayPpgAvg;
        HomeHitsAvg = gameCleaned.HomeHitsAvg;
        AwayHitsAvg = gameCleaned.AwayHitsAvg;
        HomePimAvg = gameCleaned.HomePimAvg;
        AwayPimAvg = gameCleaned.AwayPimAvg;
        HomeBlockedShotsAvg = gameCleaned.HomeBlockedShotsAvg;
        AwayBlockedShotsAvg = gameCleaned.AwayBlockedShotsAvg;
        HomeTakeawaysAvg = gameCleaned.HomeTakeawaysAvg;
        AwayTakeawaysAvg = gameCleaned.AwayTakeawaysAvg;
        HomeGiveawaysAvg = gameCleaned.HomeGiveawaysAvg;
        AwayGiveawaysAvg = gameCleaned.AwayGiveawaysAvg;
        HomeFaceOffWinPctAvg = gameCleaned.HomeFaceOffWinPctAvg;
        AwayFaceOffWinPctAvg = gameCleaned.AwayFaceOffWinPctAvg;
        HomeRecentFaceOffWinPctAvg = gameCleaned.HomeRecentFaceOffWinPctAvg;
        AwayRecentFaceOffWinPctAvg = gameCleaned.AwayRecentFaceOffWinPctAvg;
        HomeOvertimeRatio = gameCleaned.HomeOvertimeRatio;
        AwayOvertimeRatio = gameCleaned.AwayOvertimeRatio;
        HomeRecentOvertimeRatio = gameCleaned.HomeRecentOvertimeRatio;
        AwayRecentOvertimeRatio = gameCleaned.AwayRecentOvertimeRatio;
        HomeRegulationWinRatio = gameCleaned.HomeRegulationWinRatio;
        AwayRegulationWinRatio = gameCleaned.AwayRegulationWinRatio;
        HomeRecentRegulationWinRatio = gameCleaned.HomeRecentRegulationWinRatio;
        AwayRecentRegulationWinRatio = gameCleaned.AwayRecentRegulationWinRatio;
        HomePpEfficiency = gameCleaned.HomePpEfficiency;
        AwayPpEfficiency = gameCleaned.AwayPpEfficiency;
        HomeRecentPpEfficiency = gameCleaned.HomeRecentPpEfficiency;
        AwayRecentPpEfficiency = gameCleaned.AwayRecentPpEfficiency;
        HomePkEfficiency = gameCleaned.HomePkEfficiency;
        AwayPkEfficiency = gameCleaned.AwayPkEfficiency;
        HomeRecentPkEfficiency = gameCleaned.HomeRecentPkEfficiency;
        AwayRecentPkEfficiency = gameCleaned.AwayRecentPkEfficiency;
        HomeGoalsPerGamePeriod1 = gameCleaned.HomeGoalsPerGamePeriod1;
        AwayGoalsPerGamePeriod1 = gameCleaned.AwayGoalsPerGamePeriod1;
        HomeGoalsPerGamePeriod2 = gameCleaned.HomeGoalsPerGamePeriod2;
        AwayGoalsPerGamePeriod2 = gameCleaned.AwayGoalsPerGamePeriod2;
        HomeGoalsPerGamePeriod3 = gameCleaned.HomeGoalsPerGamePeriod3;
        AwayGoalsPerGamePeriod3 = gameCleaned.AwayGoalsPerGamePeriod3;
        HomeRecentGoalsPerGamePeriod1 = gameCleaned.HomeRecentGoalsPerGamePeriod1;
        AwayRecentGoalsPerGamePeriod1 = gameCleaned.AwayRecentGoalsPerGamePeriod1;
        HomeRecentGoalsPerGamePeriod2 = gameCleaned.HomeRecentGoalsPerGamePeriod2;
        AwayRecentGoalsPerGamePeriod2 = gameCleaned.AwayRecentGoalsPerGamePeriod2;
        HomeRecentGoalsPerGamePeriod3 = gameCleaned.HomeRecentGoalsPerGamePeriod3;
        AwayRecentGoalsPerGamePeriod3 = gameCleaned.AwayRecentGoalsPerGamePeriod3;
        HomeOffensiveZoneFaceoffWinPct = gameCleaned.HomeOffensiveZoneFaceoffWinPct;
        AwayOffensiveZoneFaceoffWinPct = gameCleaned.AwayOffensiveZoneFaceoffWinPct;
        HomeRecentOffensiveZoneFaceoffWinPct = gameCleaned.HomeRecentOffensiveZoneFaceoffWinPct;
        AwayRecentOffensiveZoneFaceoffWinPct = gameCleaned.AwayRecentOffensiveZoneFaceoffWinPct;
        HomePenaltyDifferentialAvg = gameCleaned.HomePenaltyDifferentialAvg;
        AwayPenaltyDifferentialAvg = gameCleaned.AwayPenaltyDifferentialAvg;
        HomeRecentPenaltyDifferentialAvg = gameCleaned.HomeRecentPenaltyDifferentialAvg;
        AwayRecentPenaltyDifferentialAvg = gameCleaned.AwayRecentPenaltyDifferentialAvg;
        HomeShootingPct = gameCleaned.HomeShootingPct;
        AwayShootingPct = gameCleaned.AwayShootingPct;
        HomeRecentShootingPct = gameCleaned.HomeRecentShootingPct;
        AwayRecentShootingPct = gameCleaned.AwayRecentShootingPct;
        HomeCorsiPct = gameCleaned.HomeCorsiPct;
        AwayCorsiPct = gameCleaned.AwayCorsiPct;
        HomeRecentCorsiPct = gameCleaned.HomeRecentCorsiPct;
        AwayRecentCorsiPct = gameCleaned.AwayRecentCorsiPct;
        HomeStrengthOfSchedule = gameCleaned.HomeStrengthOfSchedule;
        AwayStrengthOfSchedule = gameCleaned.AwayStrengthOfSchedule;
        HomeRecentStrengthOfSchedule = gameCleaned.HomeRecentStrengthOfSchedule;
        AwayRecentStrengthOfSchedule = gameCleaned.AwayRecentStrengthOfSchedule;
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
            && AwayHoursSinceLastGame == other.AwayHoursSinceLastGame
            && HomeIsBackToBack == other.HomeIsBackToBack
            && AwayIsBackToBack == other.AwayIsBackToBack
            && RestAdvantage == other.RestAdvantage
            && HomeRecentShotAttemptsAvg == other.HomeRecentShotAttemptsAvg
            && AwayRecentShotAttemptsAvg == other.AwayRecentShotAttemptsAvg
            && HomeGoalDiffAvg == other.HomeGoalDiffAvg
            && AwayGoalDiffAvg == other.AwayGoalDiffAvg
            && HomeRecentGoalDiffAvg == other.HomeRecentGoalDiffAvg
            && AwayRecentGoalDiffAvg == other.AwayRecentGoalDiffAvg
            && HomeStreak == other.HomeStreak
            && AwayStreak == other.AwayStreak
            && HomeWinRatioAtHome == other.HomeWinRatioAtHome
            && AwayWinRatioAtAway == other.AwayWinRatioAtAway
            && HeadToHeadWinRatio == other.HeadToHeadWinRatio
            && HomeSavePct == other.HomeSavePct
            && AwaySavePct == other.AwaySavePct
            && HomeRecentSavePct == other.HomeRecentSavePct
            && AwayRecentSavePct == other.AwayRecentSavePct
            && HomeSogAvg == other.HomeSogAvg
            && AwaySogAvg == other.AwaySogAvg
            && HomePpgAvg == other.HomePpgAvg
            && AwayPpgAvg == other.AwayPpgAvg
            && HomeHitsAvg == other.HomeHitsAvg
            && AwayHitsAvg == other.AwayHitsAvg
            && HomePimAvg == other.HomePimAvg
            && AwayPimAvg == other.AwayPimAvg
            && HomeBlockedShotsAvg == other.HomeBlockedShotsAvg
            && AwayBlockedShotsAvg == other.AwayBlockedShotsAvg
            && HomeTakeawaysAvg == other.HomeTakeawaysAvg
            && AwayTakeawaysAvg == other.AwayTakeawaysAvg
            && HomeGiveawaysAvg == other.HomeGiveawaysAvg
            && AwayGiveawaysAvg == other.AwayGiveawaysAvg
            && HomeFaceOffWinPctAvg == other.HomeFaceOffWinPctAvg
            && AwayFaceOffWinPctAvg == other.AwayFaceOffWinPctAvg
            && HomeRecentFaceOffWinPctAvg == other.HomeRecentFaceOffWinPctAvg
            && AwayRecentFaceOffWinPctAvg == other.AwayRecentFaceOffWinPctAvg
            && HomeOvertimeRatio == other.HomeOvertimeRatio
            && AwayOvertimeRatio == other.AwayOvertimeRatio
            && HomeRecentOvertimeRatio == other.HomeRecentOvertimeRatio
            && AwayRecentOvertimeRatio == other.AwayRecentOvertimeRatio
            && HomeRegulationWinRatio == other.HomeRegulationWinRatio
            && AwayRegulationWinRatio == other.AwayRegulationWinRatio
            && HomeRecentRegulationWinRatio == other.HomeRecentRegulationWinRatio
            && AwayRecentRegulationWinRatio == other.AwayRecentRegulationWinRatio
            && HomePpEfficiency == other.HomePpEfficiency
            && AwayPpEfficiency == other.AwayPpEfficiency
            && HomeRecentPpEfficiency == other.HomeRecentPpEfficiency
            && AwayRecentPpEfficiency == other.AwayRecentPpEfficiency
            && HomePkEfficiency == other.HomePkEfficiency
            && AwayPkEfficiency == other.AwayPkEfficiency
            && HomeRecentPkEfficiency == other.HomeRecentPkEfficiency
            && AwayRecentPkEfficiency == other.AwayRecentPkEfficiency
            && HomeGoalsPerGamePeriod1 == other.HomeGoalsPerGamePeriod1
            && AwayGoalsPerGamePeriod1 == other.AwayGoalsPerGamePeriod1
            && HomeGoalsPerGamePeriod2 == other.HomeGoalsPerGamePeriod2
            && AwayGoalsPerGamePeriod2 == other.AwayGoalsPerGamePeriod2
            && HomeGoalsPerGamePeriod3 == other.HomeGoalsPerGamePeriod3
            && AwayGoalsPerGamePeriod3 == other.AwayGoalsPerGamePeriod3
            && HomeRecentGoalsPerGamePeriod1 == other.HomeRecentGoalsPerGamePeriod1
            && AwayRecentGoalsPerGamePeriod1 == other.AwayRecentGoalsPerGamePeriod1
            && HomeRecentGoalsPerGamePeriod2 == other.HomeRecentGoalsPerGamePeriod2
            && AwayRecentGoalsPerGamePeriod2 == other.AwayRecentGoalsPerGamePeriod2
            && HomeRecentGoalsPerGamePeriod3 == other.HomeRecentGoalsPerGamePeriod3
            && AwayRecentGoalsPerGamePeriod3 == other.AwayRecentGoalsPerGamePeriod3
            && HomeOffensiveZoneFaceoffWinPct == other.HomeOffensiveZoneFaceoffWinPct
            && AwayOffensiveZoneFaceoffWinPct == other.AwayOffensiveZoneFaceoffWinPct
            && HomeRecentOffensiveZoneFaceoffWinPct == other.HomeRecentOffensiveZoneFaceoffWinPct
            && AwayRecentOffensiveZoneFaceoffWinPct == other.AwayRecentOffensiveZoneFaceoffWinPct
            && HomePenaltyDifferentialAvg == other.HomePenaltyDifferentialAvg
            && AwayPenaltyDifferentialAvg == other.AwayPenaltyDifferentialAvg
            && HomeRecentPenaltyDifferentialAvg == other.HomeRecentPenaltyDifferentialAvg
            && AwayRecentPenaltyDifferentialAvg == other.AwayRecentPenaltyDifferentialAvg
            && HomeShootingPct == other.HomeShootingPct
            && AwayShootingPct == other.AwayShootingPct
            && HomeRecentShootingPct == other.HomeRecentShootingPct
            && AwayRecentShootingPct == other.AwayRecentShootingPct
            && HomeCorsiPct == other.HomeCorsiPct
            && AwayCorsiPct == other.AwayCorsiPct
            && HomeRecentCorsiPct == other.HomeRecentCorsiPct
            && AwayRecentCorsiPct == other.AwayRecentCorsiPct
            && HomeStrengthOfSchedule == other.HomeStrengthOfSchedule
            && AwayStrengthOfSchedule == other.AwayStrengthOfSchedule
            && HomeRecentStrengthOfSchedule == other.HomeRecentStrengthOfSchedule
            && AwayRecentStrengthOfSchedule == other.AwayRecentStrengthOfSchedule;
    }
}