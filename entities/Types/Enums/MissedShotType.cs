namespace Entities.Types.Enums
{
    public enum MissedShotType
    {
        WideLeft,
        HighWideLeft,
        High,
        HighWideRight,
        WideRight,
        LeftPost,
        RightPost,
        Crossbar,
        Short
    }
    public static class MissedShotTypeParser
    {
        public static MissedShotType ParseFromString(string periodType)
        {
            switch (periodType)
            {
                case "wide-left":
                    return MissedShotType.WideLeft;
                case "high-and-wide-left":
                    return MissedShotType.HighWideLeft;
                case "wide-right":
                    return MissedShotType.WideRight;
                case "hit-left-post":
                    return MissedShotType.LeftPost;
                case "hit-right-post":
                    return MissedShotType.RightPost;
                case "above-crossbar":
                    return MissedShotType.High;
                case "short":
                    return MissedShotType.Short;
                case "hit-crossbar":
                    return MissedShotType.Crossbar;
                default:
                    throw new ArgumentException($"Invalid PeriodType value: {periodType}", nameof(periodType));
            }
        }
    }
}