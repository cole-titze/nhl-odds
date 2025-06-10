using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.DbModels
{
    public class DbPlayer : IEquatable<DbPlayer>
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
        public DbTeam? team { get; set; }
        [ForeignKey("id")]
        public DbPlayerDraftDetails? draftDetails { get; set; }
        public void Clone(DbPlayer player)
        {
            id = player.id;
            firstName = player.firstName;
            lastName = player.lastName;
            isActive = player.isActive;
            currentTeamId = player.currentTeamId;
            headShot = player.headShot;
            heroImage = player.heroImage;
            heightInInches = player.heightInInches;
            weightInPounds = player.weightInPounds;
            birthDate = player.birthDate;
            birthCity = player.birthCity;
            birthStateProvince = player.birthStateProvince;
            isInTopOneHundredAllTime = player.isInTopOneHundredAllTime;
            isInHallOfFame = player.isInHallOfFame;
            shopLink = player.shopLink;
            twitterLink = player.twitterLink;
            watchLink = player.watchLink;
            playerSlug = player.playerSlug;
            team = player.team;
            draftDetails = player.draftDetails;
        }
        public bool Equals(DbPlayer? other)
        {
            if (other == null)
                return false;

            return id == other.id
                && firstName == other.firstName
                && lastName == other.lastName
                && isActive == other.isActive
                && currentTeamId == other.currentTeamId
                && headShot == other.headShot
                && heroImage == other.heroImage
                && heightInInches == other.heightInInches
                && weightInPounds == other.weightInPounds
                && birthDate == other.birthDate
                && birthCity == other.birthCity
                && birthStateProvince == other.birthStateProvince
                && isInTopOneHundredAllTime == other.isInTopOneHundredAllTime
                && isInHallOfFame == other.isInHallOfFame
                && shopLink == other.shopLink
                && twitterLink == other.twitterLink
                && watchLink == other.watchLink
                && playerSlug == other.playerSlug;
        }
    }
}