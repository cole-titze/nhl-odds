using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.DbModels
{
    public class DbPlayer
    {
        public int id { get; set; }
        public string firstName { get; set; } = string.Empty;
        public string lastName { get; set; } = string.Empty;
        public bool isActive { get; set; }
        public int currentTeamId { get; set; }
        public string headShot { get; set; } = string.Empty;
        public string heroImage { get; set; } = string.Empty;
        public int heightInInches { get; set; }
        public int weightInPounds { get; set; }
        public DateTime birthDate { get; set; }
        public string birthCity { get; set; } = string.Empty;
        public string birthStateProvince { get; set; } = string.Empty;
        public bool isInTopOneHundredAllTime { get; set; }
        public bool isInHallOfFame { get; set; }
        public string shopLink { get; set; } = string.Empty;
        public string twitterLink { get; set; } = string.Empty;
        public string watchLink { get; set; } = string.Empty;
        public string playerSlug { get; set; } = string.Empty;
        [ForeignKey("currentTeamId")]
        public DbTeam? team { get; set; } = new DbTeam();
        public void Clone(DbPlayer playerDraftDetails)
        {
            id = playerDraftDetails.id;
            firstName = playerDraftDetails.firstName;
            lastName = playerDraftDetails.lastName;
            isActive = playerDraftDetails.isActive;
            currentTeamId = playerDraftDetails.currentTeamId;
            headShot = playerDraftDetails.headShot;
            heroImage = playerDraftDetails.heroImage;
            heightInInches = playerDraftDetails.heightInInches;
            weightInPounds = playerDraftDetails.weightInPounds;
            birthDate = playerDraftDetails.birthDate;
            birthCity = playerDraftDetails.birthCity;
            birthStateProvince = playerDraftDetails.birthStateProvince;
            isInTopOneHundredAllTime = playerDraftDetails.isInTopOneHundredAllTime;
            isInHallOfFame = playerDraftDetails.isInHallOfFame;
            shopLink = playerDraftDetails.shopLink;
            twitterLink = playerDraftDetails.twitterLink;
            watchLink = playerDraftDetails.watchLink;
            playerSlug = playerDraftDetails.playerSlug;
            team = playerDraftDetails.team;
        }

        public bool IsValid()
        {
            if (id == -1)
                return false;
            return true;
        }
    }
}