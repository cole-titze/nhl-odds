using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.DbModels
{
    public class DbPlayerDraftDetails
    {
        [Key]
        public int playerId { get; set; }
        public int year { get; set; }
        public string teamAbbrev { get; set; } = string.Empty;
        public int round { get; set; }
        public int pickInRound { get; set; }
        public int overallPick { get; set; }
        [ForeignKey(nameof(playerId))]
        public DbPlayer? player { get; set; }
        public void Clone(DbPlayerDraftDetails playerDraftDetails)
        {
            playerId = playerDraftDetails.playerId;
            year = playerDraftDetails.year;
            teamAbbrev = playerDraftDetails.teamAbbrev;
            round = playerDraftDetails.round;
            pickInRound = playerDraftDetails.pickInRound;
            overallPick = playerDraftDetails.overallPick;
        }
    }
}