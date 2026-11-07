namespace Entities.ViewModels;

public class RosterPlayerVM
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string HeadShot { get; set; } = string.Empty;
    // "C", "L", "R", "D" or "G"
    public string Position { get; set; } = string.Empty;
    public short? SweaterNumber { get; set; }
    public int GamesPlayed { get; set; }
}
