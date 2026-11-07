namespace Entities.ViewModels;

public class GamePlayerVM
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    // "C", "L", "R", "D" or "G"
    public string Position { get; set; } = string.Empty;
    public short? SweaterNumber { get; set; }
    // null before the game is played
    public PlayerGameStatsVM? Stats { get; set; }
}
