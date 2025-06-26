namespace Entities.Types.Enums
{
    public enum MissedShotType
    {
        // Wide was used in older years before having more specific left and right
        Wide,
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
                case "wide-of-net":
                    return MissedShotType.Wide;
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
                case "over-net":
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