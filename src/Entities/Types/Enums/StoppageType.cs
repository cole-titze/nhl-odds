namespace Entities.Types.Enums;

public enum StoppageType
{
    Icing,
    GoalieSave,
    Offside,
    ChallengeVisitingTeamOffside,
    ChallengeHomeTeamOffside,
    PlayerInjury,
    GoalieFreezePuck,
    PuckIntoNet,
    PuckIntoCrowd,
    TvTimeout,
    PuckIntoBenches,
    OfficialStoppage,
    SkaterFrozePuck,
    NetOff,
    VideoReview,
    HandPass,
    HighStick,
    IceProblem,
    VisitorTimeout,
    HomeTimeout,
    RinkRepair,
    ClockProblem,
    PlayerEquipment,
    ObjectsOnIce,
    PrematureSubstitution,
    OfficialInjury,
    ChallengeHomeTeamGoalInterference,
    ChallengeVisitingTeamGoalInterference,
    ChallengeLeagueOffside,
    SwitchSides,
    GoaliePuckFrozenPlayedFromBeyondCenter,
    NetDislodgedOffensiveSkater,
    NetDislodgedDefensiveSkater,
    NetDislodgedByGoaltender,
    ChallengeHomeTeamMissedStoppage,
    ChallengeVisitingTeamMissedStoppage,
    ChallengeLeagueMissedStoppage,
    ChallengeLeagueGoalInterference,
    PuckInPenaltyBenches,
    IceScrape,
    ChallengeHomeTeamPuckOverGlass,
    ChallengeVisitingTeamPuckOverGlass
}
public static class StoppageTypeParser
{
    public static StoppageType ParseFromString(string stoppageType)
    {
        switch (stoppageType)
        {
            case "icing":
                return StoppageType.Icing;
            case "goalie-stopped-after-sog":
                return StoppageType.GoalieSave;
            case "offside":
                return StoppageType.Offside;
            case "chlg-vis-off-side":
                return StoppageType.ChallengeVisitingTeamOffside;
            case "chlg-hom-off-side":
                return StoppageType.ChallengeHomeTeamOffside;
            case "player-injury":
                return StoppageType.PlayerInjury;
            case "puck-frozen":
                return StoppageType.GoalieFreezePuck;
            case "puck-in-netting":
                return StoppageType.PuckIntoNet;
            case "puck-in-crowd":
                return StoppageType.PuckIntoCrowd;
            case "tv-timeout":
                return StoppageType.TvTimeout;
            case "puck-in-benches":
                return StoppageType.PuckIntoBenches;
            case "referee-or-linesman":
                return StoppageType.OfficialStoppage;
            case "skater-puck-frozen":
                return StoppageType.SkaterFrozePuck;
            case "net-off":
                return StoppageType.NetOff;
            case "video-review":
                return StoppageType.VideoReview;
            case "hand-pass":
                return StoppageType.HandPass;
            case "high-stick":
                return StoppageType.HighStick;
            case "ice-problem":
                return StoppageType.IceProblem;
            case "visitor-timeout":
                return StoppageType.VisitorTimeout;
            case "home-timeout":
                return StoppageType.HomeTimeout;
            case "rink-repair":
                return StoppageType.RinkRepair;
            case "clock-problem":
                return StoppageType.ClockProblem;
            case "player-equipment":
                return StoppageType.PlayerEquipment;
            case "objects-on-ice":
                return StoppageType.ObjectsOnIce;
            case "premature-substitution":
                return StoppageType.PrematureSubstitution;
            case "official-injury":
                return StoppageType.OfficialInjury;
            case "chlg-hm-goal-interference":
                return StoppageType.ChallengeHomeTeamGoalInterference;
            case "chlg-vis-goal-interference":
                return StoppageType.ChallengeVisitingTeamGoalInterference;
            case "chlg-hm-off-side":
                return StoppageType.ChallengeHomeTeamOffside;
            case "chlg-league-off-side":
                return StoppageType.ChallengeLeagueOffside;
            case "switch-sides":
                return StoppageType.SwitchSides;
            case "goalie-puck-frozen-played-from-beyond-center":
                return StoppageType.GoaliePuckFrozenPlayedFromBeyondCenter;
            case "net-dislodged-offensive-skater":
                return StoppageType.NetDislodgedOffensiveSkater;
            case "net-dislodged-defensive-skater":
                return StoppageType.NetDislodgedDefensiveSkater;
            case "net-dislodged-by-goaltender":
                return StoppageType.NetDislodgedByGoaltender;
            case "chlg-hm-missed-stoppage":
                return StoppageType.ChallengeHomeTeamMissedStoppage;
            case "chlg-vis-missed-stoppage":
                return StoppageType.ChallengeVisitingTeamMissedStoppage;
            case "chlg-league-missed-stoppage":
                return StoppageType.ChallengeLeagueMissedStoppage;
            case "chlg-league-goal-interference":
                return StoppageType.ChallengeLeagueGoalInterference;
            case "puck-in-penalty-benches":
                return StoppageType.PuckInPenaltyBenches;
            case "ice-scrape":
                return StoppageType.IceScrape;
            case "chlg-hm-puck-over-glass":
                return StoppageType.ChallengeHomeTeamPuckOverGlass;
            case "chlg-vis-puck-over-glass":
                return StoppageType.ChallengeVisitingTeamPuckOverGlass;
            default:
                throw new ArgumentException($"Invalid StoppageType value: {stoppageType}", nameof(stoppageType));
        }
    }
}
public enum StoppageDetails
{
    None,
    Icing,
    GoalieSave,
    Offside,
    ChallengeVisitingTeamOffside,
    ChallengeHomeTeamOffside,
    PlayerInjury,
    GoalieFreezePuck,
    PuckIntoNet,
    PuckIntoCrowd,
    TvTimeout,
    PuckIntoBenches,
    OfficialStoppage,
    SkaterFrozePuck,
    NetOff,
    VideoReview,
    HandPass,
    HighStick,
    IceProblem,
    VisitorTimeout,
    HomeTimeout,
    RinkRepair,
    ClockProblem,
    PlayerEquipment,
    ObjectsOnIce,
    PrematureSubstitution,
    OfficialInjury,
    ChallengeHomeTeamGoalInterference,
    ChallengeVisitingTeamGoalInterference,
    ChallengeLeagueOffside,
    SwitchSides,
    GoaliePuckFrozenPlayedFromBeyondCenter,
    NetDislodgedOffensiveSkater,
    NetDislodgedDefensiveSkater,
    NetDislodgedByGoaltender,
    ChallengeHomeTeamMissedStoppage,
    ChallengeVisitingTeamMissedStoppage,
    ChallengeLeagueMissedStoppage,
    ChallengeLeagueGoalInterference,
    PuckInPenaltyBenches,
    IceScrape,
    ChallengeHomeTeamPuckOverGlass,
    ChallengeVisitingTeamPuckOverGlass,
}
public static class StoppageDetailsParser
{
    public static StoppageDetails ParseFromString(string stoppageDetails)
    {
        switch (stoppageDetails)
        {
            case "icing":
                return StoppageDetails.Icing;
            case "goalie-stopped-after-sog":
                return StoppageDetails.GoalieSave;
            case "offside":
                return StoppageDetails.Offside;
            case "chlg-vis-off-side":
                return StoppageDetails.ChallengeVisitingTeamOffside;
            case "chlg-hom-off-side":
            case "chlg-hm-off-side":
                return StoppageDetails.ChallengeHomeTeamOffside;
            case "player-injury":
                return StoppageDetails.PlayerInjury;
            case "puck-frozen":
                return StoppageDetails.GoalieFreezePuck;
            case "puck-in-netting":
                return StoppageDetails.PuckIntoNet;
            case "puck-in-crowd":
                return StoppageDetails.PuckIntoCrowd;
            case "tv-timeout":
                return StoppageDetails.TvTimeout;
            case "puck-in-benches":
                return StoppageDetails.PuckIntoBenches;
            case "referee-or-linesman":
                return StoppageDetails.OfficialStoppage;
            case "skater-puck-frozen":
                return StoppageDetails.SkaterFrozePuck;
            case "net-off":
                return StoppageDetails.NetOff;
            case "video-review":
                return StoppageDetails.VideoReview;
            case "hand-pass":
                return StoppageDetails.HandPass;
            case "high-stick":
                return StoppageDetails.HighStick;
            case "ice-problem":
                return StoppageDetails.IceProblem;
            case "visitor-timeout":
                return StoppageDetails.VisitorTimeout;
            case "home-timeout":
                return StoppageDetails.HomeTimeout;
            case "rink-repair":
                return StoppageDetails.RinkRepair;
            case "clock-problem":
                return StoppageDetails.ClockProblem;
            case "player-equipment":
                return StoppageDetails.PlayerEquipment;
            case "objects-on-ice":
                return StoppageDetails.ObjectsOnIce;
            case "premature-substitution":
                return StoppageDetails.PrematureSubstitution;
            case "official-injury":
                return StoppageDetails.OfficialInjury;
            case "chlg-hm-goal-interference":
                return StoppageDetails.ChallengeHomeTeamGoalInterference;
            case "chlg-vis-goal-interference":
                return StoppageDetails.ChallengeVisitingTeamGoalInterference;
            case "chlg-league-off-side":
                return StoppageDetails.ChallengeLeagueOffside;
            case "switch-sides":
                return StoppageDetails.SwitchSides;
            case "goalie-puck-frozen-played-from-beyond-center":
                return StoppageDetails.GoaliePuckFrozenPlayedFromBeyondCenter;
            case "net-dislodged-offensive-skater":
                return StoppageDetails.NetDislodgedOffensiveSkater;
            case "net-dislodged-defensive-skater":
                return StoppageDetails.NetDislodgedDefensiveSkater;
            case "net-dislodged-by-goaltender":
                return StoppageDetails.NetDislodgedByGoaltender;
            case "chlg-hm-missed-stoppage":
                return StoppageDetails.ChallengeHomeTeamMissedStoppage;
            case "chlg-vis-missed-stoppage":
                return StoppageDetails.ChallengeVisitingTeamMissedStoppage;
            case "chlg-league-missed-stoppage":
                return StoppageDetails.ChallengeLeagueMissedStoppage;
            case "chlg-league-goal-interference":
                return StoppageDetails.ChallengeLeagueGoalInterference;
            case "puck-in-penalty-benches":
                return StoppageDetails.PuckInPenaltyBenches;
            case "ice-scrape":
                return StoppageDetails.IceScrape;
            case "chlg-hm-puck-over-glass":
                return StoppageDetails.ChallengeHomeTeamPuckOverGlass;
            case "chlg-vis-puck-over-glass":
                return StoppageDetails.ChallengeVisitingTeamPuckOverGlass;
            case "":
                return StoppageDetails.None;
            default:
                throw new ArgumentException($"Invalid StoppageDetails value: {stoppageDetails}", nameof(stoppageDetails));
        }
    }
}