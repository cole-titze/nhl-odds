namespace Entities.Types.Enums
{
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
        IllegalGoaliePlay
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
                default:
                    throw new ArgumentException($"Invalid PenaltyType value: {penaltyType}", nameof(penaltyType));
            }
        }
    }
}