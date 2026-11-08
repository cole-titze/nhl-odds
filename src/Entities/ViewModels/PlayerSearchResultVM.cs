namespace Entities.ViewModels;

/// <summary>A player found by name (FindPlayers): enough to pick the right one and get their id.</summary>
public class PlayerSearchResultVM
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    // "C", "L", "R", "D" or "G" from their latest game; null with no games
    public string? Position { get; set; }
    // The team they're on now (the NHL's roster report); null when on no NHL roster
    public int? CurrentTeamId { get; set; }
    public string? CurrentTeamAbbreviation { get; set; }
    // The team of their latest game
    public string? LastTeamAbbreviation { get; set; }
    // Played this season
    public bool IsActive { get; set; }
}
