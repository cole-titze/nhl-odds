using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.DbModels;

public class DbPlayer
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
    public bool IsInTopOneHundredAllTime { get; set; }
    public bool IsInHallOfFame { get; set; }
    public string ShopLink { get; set; } = string.Empty;
    public string TwitterLink { get; set; } = string.Empty;
    public string WatchLink { get; set; } = string.Empty;
    public string PlayerSlug { get; set; } = string.Empty;
    [ForeignKey(nameof(CurrentTeamId))]
    public DbTeam? Team { get; set; }
    public void Clone(DbPlayer player)
    {
        Id = player.Id;
        FirstName = player.FirstName;
        LastName = player.LastName;
        IsActive = player.IsActive;
        CurrentTeamId = player.CurrentTeamId;
        HeadShot = player.HeadShot;
        HeroImage = player.HeroImage;
        HeightInInches = player.HeightInInches;
        WeightInPounds = player.WeightInPounds;
        BirthDate = player.BirthDate;
        BirthCity = player.BirthCity;
        BirthStateProvince = player.BirthStateProvince;
        IsInTopOneHundredAllTime = player.IsInTopOneHundredAllTime;
        IsInHallOfFame = player.IsInHallOfFame;
        ShopLink = player.ShopLink;
        TwitterLink = player.TwitterLink;
        WatchLink = player.WatchLink;
        PlayerSlug = player.PlayerSlug;
        Team = player.Team;
    }
    public bool Equals(DbPlayer? other)
    {
        if (other == null)
            return false;

        return Id == other.Id
            && FirstName == other.FirstName
            && LastName == other.LastName
            && IsActive == other.IsActive
            && CurrentTeamId == other.CurrentTeamId
            && HeadShot == other.HeadShot
            && HeroImage == other.HeroImage
            && HeightInInches == other.HeightInInches
            && WeightInPounds == other.WeightInPounds
            && BirthDate == other.BirthDate
            && BirthCity == other.BirthCity
            && BirthStateProvince == other.BirthStateProvince
            && IsInTopOneHundredAllTime == other.IsInTopOneHundredAllTime
            && IsInHallOfFame == other.IsInHallOfFame
            && ShopLink == other.ShopLink
            && TwitterLink == other.TwitterLink
            && WatchLink == other.WatchLink
            && PlayerSlug == other.PlayerSlug;
    }
}