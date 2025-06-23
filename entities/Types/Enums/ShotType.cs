namespace Entities.Types.Enums
{
    public enum ShotType
    {
        Wrist,
        Slap,
        Snap,
        Tip,
        Backhand,
        WrapAround,
        Deflected
    }
    public static class ShotTypeParser
    {
        public static ShotType ParseFromString(string shotType)
        {
            switch (shotType)
            {
                case "wrist":
                    return ShotType.Wrist;
                case "slap":
                    return ShotType.Slap;
                case "snap":
                    return ShotType.Snap;
                case "tip-in":
                    return ShotType.Tip;
                case "backhand":
                    return ShotType.Backhand;
                case "wrap-around":
                    return ShotType.WrapAround;
                case "deflected":
                    return ShotType.Deflected;
            default:
                throw new ArgumentException($"Invalid PeriodType value: {shotType}", nameof(shotType));
            }
        }
    }
}