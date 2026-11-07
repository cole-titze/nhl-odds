using Entities.Types;

namespace Entities.Models.Web;

/// <summary>A player's bio for the Player page, with the team, position and number of his most recent game.</summary>
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
    // From his most recent game; null when he has none
    public POSITION? Position { get; set; }
    public short? SweaterNumber { get; set; }
    public Team? CurrentTeam { get; set; }
}
