namespace Entities.Types.Enums
{
    public enum Zone
    {
        Offensive,
        Defensive,
        Neutral
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
            default:
                throw new ArgumentException($"Invalid PeriodType value: {zoneType}", nameof(zoneType));
            }
        }
    }
}