namespace Entities.Models;

public class Player
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int CurrentTeamId { get; set; }
    public string HeadShot { get; set; } = string.Empty;
    public string HeroImage { get; set; } = string.Empty;
    public int HeightInInches { get; set; }
    public int WeightInPounds { get; set; }
    public DateTime BirthDate { get; set; }
    public string BirthCity { get; set; } = string.Empty;
    public string BirthStateProvince { get; set; } = string.Empty;
    public string BirthCountry { get; set; } = string.Empty;
    public bool IsInTopOneHundredAllTime { get; set; }
    public bool IsInHallOfFame { get; set; }
    public string ShopLink { get; set; } = string.Empty;
    public string TwitterLink { get; set; } = string.Empty;
    public string WatchLink { get; set; } = string.Empty;
    public string PlayerSlug { get; set; } = string.Empty;
    public PlayerDraftDetails? PlayerDraftDetails { get; set; }
}