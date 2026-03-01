namespace Entities.Types.Enums;

public enum MissedShotType
{
    #region Older Years Shot Types
    Unknown = -1,
    Wide = 0,
    GoalPost = 1,
    #endregion

    WideLeft = 2,
    HighWideLeft = 3,
    High = 4,
    HighWideRight = 5,
    WideRight = 6,
    LeftPost = 7,
    RightPost = 8,
    Crossbar = 9,
    Short = 10,
}
public static class MissedShotTypeParser
{
    public static MissedShotType ParseFromString(string missedShotType)
    {
        switch (missedShotType)
        {
            case "wide-of-net":
                return MissedShotType.Wide;
            case "wide-left":
                return MissedShotType.WideLeft;
            case "high-and-wide-left":
                return MissedShotType.HighWideLeft;
            case "high-and-wide-right":
                return MissedShotType.HighWideRight;
            case "wide-right":
                return MissedShotType.WideRight;
            case "hit-left-post":
                return MissedShotType.LeftPost;
            case "hit-right-post":
                return MissedShotType.RightPost;
            case "above-crossbar":
            case "over-net":
                return MissedShotType.High;
            case "short":
                return MissedShotType.Short;
            case "hit-crossbar":
                return MissedShotType.Crossbar;
            case "goalpost":
                return MissedShotType.GoalPost;
            case null:
                return MissedShotType.Unknown;
            default:
                throw new ArgumentException($"Invalid MissedShot value: {missedShotType}", nameof(missedShotType));
        }
    }
}