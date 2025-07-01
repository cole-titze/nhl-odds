using Entities.DbModels;
using Entities.Models;

namespace Entities.Mappers.PlayerMappers;

public static class MapPlayerToDbPlayer
{
    public static DbPlayer Map(Player player)
    {
        return new DbPlayer()
        {
            Id = player.Id,
            FirstName = player.FirstName,
            LastName = player.LastName,
            IsActive = player.IsActive,
            CurrentTeamId = player.CurrentTeamId,
            HeadShot = player.HeadShot,
            HeroImage = player.HeroImage,
            HeightInInches = player.HeightInInches,
            WeightInPounds = player.WeightInPounds,
            BirthDate = player.BirthDate,
            BirthCity = player.BirthCity,
            BirthStateProvince = player.BirthStateProvince,
            IsInTopOneHundredAllTime = player.IsInTopOneHundredAllTime,
            IsInHallOfFame = player.IsInHallOfFame,
            ShopLink = player.ShopLink,
            TwitterLink = player.TwitterLink,
            WatchLink = player.WatchLink,
            PlayerSlug = player.PlayerSlug,

        };
    }
    public static IEnumerable<DbPlayer> Map(IEnumerable<Player> players)
    {
        var dbPlayers = new List<DbPlayer>();
        foreach (var player in players)
        {
            dbPlayers.Add(Map(player));
        }
        return dbPlayers;
    }
}

