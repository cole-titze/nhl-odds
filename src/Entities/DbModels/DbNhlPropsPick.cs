namespace Entities.DbModels;

/// <summary>
/// One player pick from a version of NHL.com's daily picks and props article, e.g. "Player to watch for point,
/// shots on goal and/or power-play point: Jackson Blake, F, CAR (at CHI)".
/// </summary>
public class DbNhlPropsPick
{
    public string EntityId { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    // "Player to watch for point, assist and/or power-play point", "Others to watch for point", "Goal Chase", "Fantasy Stars"
    public string Category { get; set; } = string.Empty;
    public int PlayerId { get; set; }
    // The day the picks are for, from the section heading ("NHL PROPS: SAT. OCT. 10")
    public DateTime? PickDate { get; set; }
    // F, D or G
    public string Position { get; set; } = string.Empty;
    public string TeamAbbreviation { get; set; } = string.Empty;
    public string OpponentAbbreviation { get; set; } = string.Empty;
    public bool IsHome { get; set; }
}