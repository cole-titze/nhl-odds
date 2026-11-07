namespace Entities.ViewModels;

/// <summary>A player found by name (FindPlayers): enough to pick the right one and get his id.</summary>
public class PlayerSearchResultVM
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    // "C", "L", "R", "D" or "G" from his latest game; null with no games
    public string? Position { get; set; }
    // The team of his latest game
    public int? CurrentTeamId { get; set; }
    public string? CurrentTeamAbbreviation { get; set; }
    // Played this season
    public bool IsActive { get; set; }
}
