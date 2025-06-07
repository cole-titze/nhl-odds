namespace Entities.Models
{
    public class Player
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
        public PlayerDraftDetails? MyProperty { get; set; }
    }
}