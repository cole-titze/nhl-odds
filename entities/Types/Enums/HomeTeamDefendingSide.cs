namespace Entities.Types.Enums;

public enum HomeTeamDefendingSide
{
    Unknown = -1,
    Left = 0,
    Right = 1,
}
public static class HomeTeamDefendingSideParser
{
    public static HomeTeamDefendingSide ParseFromString(string? side)
    {
        switch (side)
        {
            case "left":
                return HomeTeamDefendingSide.Left;
            case "right":
                return HomeTeamDefendingSide.Right;
            case null:
                return HomeTeamDefendingSide.Unknown;
            default:
                throw new ArgumentException($"Invalid HomeTeamDefendingSide value: {side}", nameof(side));
        }
    }
}