namespace Entities.Types.Enums;

public enum ModeType
{
    NhlAdd,
    NhlUpdate,
    NextDayOdds,
    BackfillOdds,
    KalshiFetch,
    BackfillKalshi,
    BackfillGame,
    BackfillGoalieStats,
    CleanAll,
    LineupSnapshot
}
public static class ModeTypeParser
{
    public static ModeType ParseFromString(string? modeType)
    {
        switch (modeType)
        {
            case "NhlUpdate":
                return ModeType.NhlUpdate;
            case "NhlAdd":
            case null:
                return ModeType.NhlAdd;
            case "NextDayOdds":
                return ModeType.NextDayOdds;
            case "BackfillOdds":
                return ModeType.BackfillOdds;
            case "KalshiFetch":
                return ModeType.KalshiFetch;
            case "BackfillKalshi":
                return ModeType.BackfillKalshi;
            case "BackfillGame":
                return ModeType.BackfillGame;
            case "BackfillGoalieStats":
                return ModeType.BackfillGoalieStats;
            case "CleanAll":
                return ModeType.CleanAll;
            case "LineupSnapshot":
                return ModeType.LineupSnapshot;
            default:
                throw new ArgumentException($"Invalid ModeType value: {modeType}", nameof(modeType));
        }
    }
}