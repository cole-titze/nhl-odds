namespace Entities.Models;

public class PlayerDraftDetails
{
    public int Year { get; set; }
    public string TeamAbbrev { get; set; } = string.Empty;
    public int Round { get; set; }
    public int PickInRound { get; set; }
    public int OverallPick { get; set; }
}