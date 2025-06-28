using Entities.DbModels;
using Entities.Models;

namespace DataAccess.PlayerRepository.Mappers
{
    public static class MapPlayerToDbPlayer
    {
        public static DbPlayer Map(Player player)
        {
            return new DbPlayer()
            {
                id = player.id,
                firstName = player.firstName,
                lastName = player.lastName,
                isActive = player.isActive,
                currentTeamId = player.currentTeamId,
                headShot = player.headShot,
                heroImage = player.heroImage,
                heightInInches = player.heightInInches,
                weightInPounds = player.weightInPounds,
                birthDate = player.birthDate,
                birthCity = player.birthCity,
                birthStateProvince = player.birthStateProvince,
                isInTopOneHundredAllTime = player.isInTopOneHundredAllTime,
                isInHallOfFame = player.isInHallOfFame,
                shopLink = player.shopLink,
                twitterLink = player.twitterLink,
                watchLink = player.watchLink,
                playerSlug = player.playerSlug,

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
}

