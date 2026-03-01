namespace Entities.Types.Enums;

public enum ShotType
{
    Unknown = -1,
    Wrist = 0,
    Slap = 1,
    Snap = 2,
    Tip = 3,
    Backhand = 4,
    WrapAround = 5,
    Deflected = 6,
    Bat = 7,
    Poke = 8,
    BetweenLegs = 9
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
            case "bat":
                return ShotType.Bat;
            case "poke":
                return ShotType.Poke;
            case "between-legs":
                return ShotType.BetweenLegs;
            case null:
                return ShotType.Unknown;
            default:
                throw new ArgumentException($"Invalid ShotType value: {shotType}", nameof(shotType));
        }
    }
}