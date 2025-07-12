using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.DbModels;

public class DbPlayerDraftDetails
{
    [Key]
    public int PlayerId { get; set; }
    public int Year { get; set; }
    public string TeamAbbrev { get; set; } = string.Empty;
    public int Round { get; set; }
    public int PickInRound { get; set; }
    public int OverallPick { get; set; }
    [ForeignKey(nameof(PlayerId))]
    public DbPlayer? Player { get; set; }
    public void Clone(DbPlayerDraftDetails playerDraftDetails)
    {
        PlayerId = playerDraftDetails.PlayerId;
        Year = playerDraftDetails.Year;
        TeamAbbrev = playerDraftDetails.TeamAbbrev;
        Round = playerDraftDetails.Round;
        PickInRound = playerDraftDetails.PickInRound;
        OverallPick = playerDraftDetails.OverallPick;
    }
    public bool IsEquivalentTo(DbPlayerDraftDetails? other)
    {
        if (other == null)
            return false;

        return PlayerId == other.PlayerId
            && Year == other.Year
            && TeamAbbrev == other.TeamAbbrev
            && Round == other.Round
            && PickInRound == other.PickInRound
            && OverallPick == other.OverallPick;
    }
}