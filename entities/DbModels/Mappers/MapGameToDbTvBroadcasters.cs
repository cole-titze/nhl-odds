using Entities.Models;

namespace Entities.DbModels.Mappers
{
    public static class MapGameToDbTvBroadcasters
    {
        public static IEnumerable<DbTvBroadcaster> Map(Game game)
        {
            if (game.extendedInfo == null || game.extendedInfo.tvBroadcasters == null)
                throw new ArgumentNullException(nameof(game.extendedInfo), "Game extended info or TV broadcasters cannot be null.");

            var broadcasters = new List<DbTvBroadcaster>();
            foreach (var broadcaster in game.extendedInfo.tvBroadcasters)
            {
                var dbBroadcaster = new DbTvBroadcaster()
                {
                    id = broadcaster.id,
                    networkName = broadcaster.networkName,
                    marketAbbreviation = broadcaster.marketAbbreviation,
                    sequenceNumber = broadcaster.sequenceNumber,
                    countryCode = broadcaster.countryCode
                };
                broadcasters.Add(dbBroadcaster);
            }

            return broadcasters;
        }
        public static IEnumerable<DbTvBroadcaster> MapList(IEnumerable<Game> games)
        {
            var broadcasters = new List<DbTvBroadcaster>();
            foreach (var game in games)
            {
                var gameBroadcasters = Map(game);
                broadcasters.AddRange(gameBroadcasters);
            }

            return broadcasters;
        }
    }
}