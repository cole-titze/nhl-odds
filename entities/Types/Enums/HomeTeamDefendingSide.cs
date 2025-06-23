namespace Entities.Types.Enums
{
    public enum HomeTeamDefendingSide
    {
        Left,
        Right
    }
    public static class HomeTeamDefendingSideParser
    {
        public static HomeTeamDefendingSide ParseFromString(string side)
        {
            if (side == "left")
                return HomeTeamDefendingSide.Left;
            if (side == "right")
                return HomeTeamDefendingSide.Right;
                
            throw new ArgumentException($"Invalid HomeTeamDefendingSide value: {side}", nameof(side));
        }
    }
}