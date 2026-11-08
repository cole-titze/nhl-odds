using Entities.Types;

namespace Entities.Models.Web;

/// <summary>A player's bio for the Player page: their current team, and the team, position and number of their most recent game.</summary>
public class PlayerProfile
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string HeadShot { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }
    public string BirthCity { get; set; } = string.Empty;
    public string BirthStateProvince { get; set; } = string.Empty;
    public int HeightInInches { get; set; }
    public int WeightInPounds { get; set; }
    // From their most recent game; null when they have none
    public POSITION? Position { get; set; }
    public short? SweaterNumber { get; set; }
    // The team listing them on the NHL's latest roster report (RosterStatus), or, when there's no recent report,
    // the team of their latest game if it was this season. Null when they're on no NHL roster (sent down,
    // unsigned, retired)
    public Team? CurrentTeam { get; set; }
    // On that team's injured reserve list
    public bool IsInjuredReserve { get; set; }
    // The team of their most recent game, and the season it was in; null with no games
    public Team? LastTeam { get; set; }
    public int? LastSeasonStartYear { get; set; }
}
