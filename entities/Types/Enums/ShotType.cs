namespace Entities.Types.Enums
{
    public enum ShotType
    {
        Wrist,
        Slap,
        Backhand
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
            case "backhand":
                return ShotType.Backhand;
            default:
                throw new ArgumentException($"Invalid PeriodType value: {shotType}", nameof(shotType));
            }
        }
    }
}