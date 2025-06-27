namespace Entities.Types.Enums
{
    public enum Zone
    {
        Unknown = -1,
        Offensive = 0,
        Defensive = 1,
        Neutral = 2,
    }
    public static class ZoneParser
    {
        public static Zone ParseFromString(string zoneType)
        {
            switch (zoneType)
            {
                case "O":
                    return Zone.Offensive;
                case "N":
                    return Zone.Neutral;
                case "D":
                    return Zone.Defensive;
                case null:
                    return Zone.Unknown;
                default:
                    throw new ArgumentException($"Invalid PeriodType value: {zoneType}", nameof(zoneType));
            }
        }
    }
}