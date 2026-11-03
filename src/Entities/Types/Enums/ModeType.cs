namespace Entities.Types.Enums;

public enum ModeType
{
    NhlAdd,
    NhlUpdate,
    NextDayOdds,
    BackfillOdds,
    ClosingOdds,
    BackfillClosingOdds,
    NextDayKalshi,
    BackfillKalshi,
    CleanAll,
    LineupSnapshot,
    SearchIndex,
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
            case "ClosingOdds":
                return ModeType.ClosingOdds;
            case "BackfillClosingOdds":
                return ModeType.BackfillClosingOdds;
            case "NextDayKalshi":
                return ModeType.NextDayKalshi;
            case "BackfillKalshi":
                return ModeType.BackfillKalshi;
            case "CleanAll":
                return ModeType.CleanAll;
            case "LineupSnapshot":
                return ModeType.LineupSnapshot;
            case "SearchIndex":
                return ModeType.SearchIndex;
            default:
                throw new ArgumentException($"Invalid ModeType value: {modeType}", nameof(modeType));
        }
    }
}