namespace Entities.ViewModels;

/// <summary>The Player page: bio, current team, and their games and totals in one season.</summary>
public class PlayerVM
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string HeadShot { get; set; } = string.Empty;
    // "C", "L", "R", "D" or "G" from their most recent game; null when they have none
    public string? Position { get; set; }
    public short? SweaterNumber { get; set; }
    // yyyy-MM-dd
    public string? BirthDate { get; set; }
    public string BirthCity { get; set; } = string.Empty;
    public string BirthStateProvince { get; set; } = string.Empty;
    public int HeightInInches { get; set; }
    public int WeightInPounds { get; set; }
    // The team listing them on the NHL's latest roster report (or, with no recent report, the team of their latest
    // game if it was this season); null when they're on no NHL roster: then show LastTeam as "last played for"
    public PlayerTeamVM? CurrentTeam { get; set; }
    // On CurrentTeam's injured reserve list
    public bool IsInjuredReserve { get; set; }
    // The team of their most recent game and that game's season; null with no games
    public PlayerTeamVM? LastTeam { get; set; }
    public int? LastSeasonStartYear { get; set; }
    public int SeasonStartYear { get; set; }
    // Newest first
    public IEnumerable<PlayerGameVM> Games { get; set; } = new List<PlayerGameVM>();
    public PlayerSeasonTotalsVM SeasonTotals { get; set; } = new();
}
