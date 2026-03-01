namespace Entities.Types.Enums;

public enum PenaltyType
{
    Tripping,
    Slashing,
    Hooking,
    Roughing,
    RoughingRemovingOpponentsHelmet,
    DelayOfGamePuckOverGlass,
    DelayOfGameFailedChallenge,
    Interference,
    TooManyMen,
    HighStick,
    HighStickDoubleMinor,
    Fighting,
    Holding,
    Boarding,
    UnsportsmanlikeConduct,
    HoldingTheStick,
    CrossCheck,
    GoaltenderInterfence,
    Elbow,
    FightInstigator,
    Misconduct,
    IllegalGoaliePlay,
    Charging,
    GameMisconduct,
    Kneeing,
    Diving,
    FightInstigatorWithFaceShield, // Keeping on visor for a fight is additional penalty time
    HandPuck,
    PenaltyShotTripOnBreakaway,
    DelayOfGame,
    PenaltyShotHookOnBreakaway,
    AbuseOfOfficials,
    ThrowingEquipment,
    Clipping,
    InstigatorMisconduct,
    CheckingFromBehind,
    InterferenceWithOfficial,
    Spearing,
    Aggressor,
    DelayOfGameSmotheringPuck,
    MatchPenalty,
    BrokenStick,
    IllegalSubstitution,
    HeadButt,
    PenaltyShotSlashOnBreakaway,
    GoalieLeftCrease,
    IllegalStick,
    AbusiveLanguage,
    SpearingDoubleMinor,
    CrossCheckDoubleMinor,
    IllegalEquipment,
    PenaltyShotHoldingOnBreakaway,
    Embellishment,
    GameMisconductHeadCoach,
    CoachOnIce,
    IllegalCheckToHead,
    DelayOfGameFacoffViolation,
    DelayOfGameBenchFacoffViolation,
    PenaltyShotCoveringPuckInCrease,
    ButtEnding,
    DelayOfGameBench,
    PenaltyShotThrowingObjectAtPuck,
    ObjectsOnIce,
    PenaltyShotNetDisplaced,
    LeavingPenaltyBench,
    ConcealingPuck,
    GameMisconductTeamStaff,
    PenaltyShotHoldingStickOnBreakaway,
    Bench,
    PlayerLeavesBench,
    LeavingPenaltyBox,
    Minor,
    Major,
    HeadButtDoubleMinor,
    PenaltyShotGoalkeeperDisplacedNet,
    ButtEndingDoubleMinor,
    PlayingWithoutHelmet,
    DelayOfGameFailedChallengeDoubleMinor,
    InterferenceBench,
    UnsportsmanlikeConductBench,
    GoalieRemovedOwnMask,
    PuckThrownForwardGoalkeeper,
    DelayOfGameEquipment
}
public static class PenaltyTypeParser
{
    public static PenaltyType ParseFromString(string penaltyType)
    {
        switch (penaltyType)
        {
            case "tripping":
                return PenaltyType.Tripping;
            case "delaying-game-puck-over-glass":
                return PenaltyType.DelayOfGamePuckOverGlass;
            case "hooking":
                return PenaltyType.Hooking;
            case "roughing":
                return PenaltyType.Roughing;
            case "interference":
                return PenaltyType.Interference;
            case "slashing":
                return PenaltyType.Slashing;
            case "too-many-men-on-the-ice":
                return PenaltyType.TooManyMen;
            case "high-sticking":
                return PenaltyType.HighStick;
            case "fighting":
                return PenaltyType.Fighting;
            case "holding":
                return PenaltyType.Holding;
            case "boarding":
                return PenaltyType.Boarding;
            case "roughing-removing-opponents-helmet":
                return PenaltyType.RoughingRemovingOpponentsHelmet;
            case "unsportsmanlike-conduct":
                return PenaltyType.UnsportsmanlikeConduct;
            case "delaying-game-unsuccessful-challenge":
                return PenaltyType.DelayOfGameFailedChallenge;
            case "holding-the-stick":
                return PenaltyType.HoldingTheStick;
            case "cross-checking":
                return PenaltyType.CrossCheck;
            case "interference-goalkeeper":
                return PenaltyType.GoaltenderInterfence;
            case "high-sticking-double-minor":
                return PenaltyType.HighStickDoubleMinor;
            case "elbowing":
                return PenaltyType.Elbow;
            case "instigator":
                return PenaltyType.FightInstigator;
            case "misconduct":
                return PenaltyType.Misconduct;
            case "delaying-game-illegal-play-by-goalie":
                return PenaltyType.IllegalGoaliePlay;
            case "charging":
                return PenaltyType.Charging;
            case "game-misconduct":
                return PenaltyType.GameMisconduct;
            case "kneeing":
                return PenaltyType.Kneeing;
            case "diving":
                return PenaltyType.Diving;
            case "instigator-face-shield":
                return PenaltyType.FightInstigatorWithFaceShield;
            case "closing-hand-on-puck":
                return PenaltyType.HandPuck;
            case "ps-tripping-on-breakaway":
                return PenaltyType.PenaltyShotTripOnBreakaway;
            case "ps-hooking-on-breakaway":
                return PenaltyType.PenaltyShotHookOnBreakaway;
            case "delaying-game":
                return PenaltyType.DelayOfGame;
            case "abuse-of-officials":
                return PenaltyType.AbuseOfOfficials;
            case "throwing-equipment":
                return PenaltyType.ThrowingEquipment;
            case "clipping":
                return PenaltyType.Clipping;
            case "instigator-misconduct":
                return PenaltyType.InstigatorMisconduct;
            case "checking-from-behind":
                return PenaltyType.CheckingFromBehind;
            case "interference-with-official":
                return PenaltyType.InterferenceWithOfficial;
            case "spearing":
                return PenaltyType.Spearing;
            case "aggressor":
                return PenaltyType.Aggressor;
            case "delaying-game-smothering-puck":
                return PenaltyType.DelayOfGameSmotheringPuck;
            case "match-penatly-10-minutes":
            case "match-penalty":
                return PenaltyType.MatchPenalty;
            case "broken-stick":
                return PenaltyType.BrokenStick;
            case "illegal-substitution":
                return PenaltyType.IllegalSubstitution;
            case "head-butting":
                return PenaltyType.HeadButt;
            case "ps-slash-on-breakaway":
                return PenaltyType.PenaltyShotSlashOnBreakaway;
            case "goalie-leave-crease":
                return PenaltyType.GoalieLeftCrease;
            case "illegal-stick":
                return PenaltyType.IllegalStick;
            case "abusive-language":
                return PenaltyType.AbusiveLanguage;
            case "spearing-double-minor":
                return PenaltyType.SpearingDoubleMinor;
            case "cross-checking-double-minor":
                return PenaltyType.CrossCheckDoubleMinor;
            case "illegal-equipment":
                return PenaltyType.IllegalEquipment;
            case "ps-holding-on-breakaway":
                return PenaltyType.PenaltyShotHoldingOnBreakaway;
            case "embellishment":
                return PenaltyType.Embellishment;
            case "game-misconduct-head-coach":
                return PenaltyType.GameMisconductHeadCoach;
            case "coach-or-manager-on-the-ice":
                return PenaltyType.CoachOnIce;
            case "illegal-check-to-head":
                return PenaltyType.IllegalCheckToHead;
            case "delaying-game-face-off-violation":
                return PenaltyType.DelayOfGameFacoffViolation;
            case "delaying-game-bench-face-off-violation":
                return PenaltyType.DelayOfGameBenchFacoffViolation;
            case "ps-covering-puck-in-crease":
                return PenaltyType.PenaltyShotCoveringPuckInCrease;
            case "butt-ending":
                return PenaltyType.ButtEnding;
            case "delaying-game-bench":
                return PenaltyType.DelayOfGameBench;
            case "ps-throwing-object-at-puck":
                return PenaltyType.PenaltyShotThrowingObjectAtPuck;
            case "objects-on-ice":
                return PenaltyType.ObjectsOnIce;
            case "ps-net-displaced":
                return PenaltyType.PenaltyShotNetDisplaced;
            case "leaving-players-penalty-bench":
                return PenaltyType.LeavingPenaltyBench;
            case "concealing-puck":
                return PenaltyType.ConcealingPuck;
            case "game-misconduct-team-staff":
                return PenaltyType.GameMisconductTeamStaff;
            case "ps-holding-stick-on-breakaway":
                return PenaltyType.PenaltyShotHoldingStickOnBreakaway;
            case "bench":
                return PenaltyType.Bench;
            case "player-leaves-bench":
                return PenaltyType.PlayerLeavesBench;
            case "leaving-penalty-box":
                return PenaltyType.LeavingPenaltyBox;
            case "minor":
                return PenaltyType.Minor;
            case "major":
                return PenaltyType.Major;
            case "head-butting-double-minor":
                return PenaltyType.HeadButtDoubleMinor;
            case "ps-goalkeeper-displaced-net":
                return PenaltyType.PenaltyShotGoalkeeperDisplacedNet;
            case "butt-ending-double-minor":
                return PenaltyType.ButtEndingDoubleMinor;
            case "playing-without-a-helmet":
                return PenaltyType.PlayingWithoutHelmet;
            case "delaying-game-unsuccessful-challenge-double-minor":
                return PenaltyType.DelayOfGameFailedChallengeDoubleMinor;
            case "interference-bench":
                return PenaltyType.InterferenceBench;
            case "unsportsmanlike-conduct-bench":
                return PenaltyType.UnsportsmanlikeConductBench;
            case "goalie-removed-own-mask":
                return PenaltyType.GoalieRemovedOwnMask;
            case "puck-thrown-forward-goalkeeper":
                return PenaltyType.PuckThrownForwardGoalkeeper;
            case "delaying-game-equipment":
                return PenaltyType.DelayOfGameEquipment;
            default:
                throw new ArgumentException($"Invalid PenaltyType value: {penaltyType}", nameof(penaltyType));
        }
    }
}