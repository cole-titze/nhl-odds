namespace Entities.ViewModels;

/// <summary>The Player page: bio, current team, and his games and totals in one season.</summary>
public class PlayerVM
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string HeadShot { get; set; } = string.Empty;
    // "C", "L", "R", "D" or "G" from his most recent game; null when he has none
    public string? Position { get; set; }
    public short? SweaterNumber { get; set; }
    // yyyy-MM-dd
    public string? BirthDate { get; set; }
    public string BirthCity { get; set; } = string.Empty;
    public string BirthStateProvince { get; set; } = string.Empty;
    public int HeightInInches { get; set; }
    public int WeightInPounds { get; set; }
    // The team of his most recent game
    public PlayerTeamVM? CurrentTeam { get; set; }
    public int SeasonStartYear { get; set; }
    // Newest first
    public IEnumerable<PlayerGameVM> Games { get; set; } = new List<PlayerGameVM>();
    public PlayerSeasonTotalsVM SeasonTotals { get; set; } = new();
}
