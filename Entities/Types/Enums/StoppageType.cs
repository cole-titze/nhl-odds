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
    ChallengeHomeTeamMissedStoppage
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
            default:
                throw new ArgumentException($"Invalid StoppageType value: {stoppageType}", nameof(stoppageType));
        }
    }
}
public enum StoppageDetails
{
    None,
    TvTimeout,
    PlayerInjury,
    OfficialInjury,
}
public static class StoppageDetailsParser
{
    public static StoppageDetails ParseFromString(string stoppageDetails)
    {
        switch (stoppageDetails)
        {
            case "tv-timeout":
                return StoppageDetails.TvTimeout;
            case "official-injury":
                return StoppageDetails.OfficialInjury;
            case "player-injury":
                return StoppageDetails.PlayerInjury;
            case "":
                return StoppageDetails.None;
            default:
                throw new ArgumentException($"Invalid StoppageDetails value: {stoppageDetails}", nameof(stoppageDetails));
        }
    }
}