namespace Entities.DbModels;

/// <summary>
/// One player in a projected lineup: a forward line, defense pair or goalie, or the scratched, injured or suspended list.
/// </summary>
public class DbLineupPlayer
{
    public string SectionHash { get; set; } = string.Empty;
    // "Away" or "Home"
    public string Side { get; set; } = string.Empty;
    // F, D, G, Scratched, Injured or Suspended
    public string Group { get; set; } = string.Empty;
    // Forward line, defense pair or goalie order (1 is listed first); list order for the other groups
    public int LineNumber { get; set; }
    // Place on the line: forwards 1-3 (left wing, center, right wing), defense 1-2; 1 for the other groups
    public int Slot { get; set; }
    public int? TeamId { get; set; }
    // The name as written
    public string Name { get; set; } = string.Empty;
    // Null when the name didn't match exactly one player on the team's NHL roster
    public int? PlayerId { get; set; }
    // The injury for injured players, e.g. "upper body"
    public string? Note { get; set; }
}