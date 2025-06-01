namespace Entities.Models
{
    public class PlayerDraftDetails
    {
        public int year { get; set; }
        public string teamAbbrev { get; set; } = string.Empty;
        public int round { get; set; }
        public int pickInRound { get; set; }
        public int overallPick { get; set; }
    }
}