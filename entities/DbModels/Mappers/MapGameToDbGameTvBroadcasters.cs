using Entities.Models;

namespace Entities.DbModels.Mappers
{
    public static class MapGameToDbGameTvBroadcasters
    {
        public static IEnumerable<DbGameTvBroadcaster> Map(Game game)
        {
            if (game.extendedInfo == null || game.extendedInfo.tvBroadcasters == null)
                throw new ArgumentNullException(nameof(game.extendedInfo), "Game extended info or TV broadcasters cannot be null.");

            var broadcasters = new List<DbGameTvBroadcaster>();
            foreach (var broadcaster in game.extendedInfo.tvBroadcasters)
            {
                var dbBroadcaster = new DbGameTvBroadcaster()
                {
                    gameId = game.id,
                    broadcasterId = broadcaster.id,
                };
                broadcasters.Add(dbBroadcaster);
            }

            return broadcasters;
        }
        public static IEnumerable<DbGameTvBroadcaster> MapList(IEnumerable<Game> games)
        {
            var broadcasters = new List<DbGameTvBroadcaster>();
            foreach (var game in games)
            {
                var gameBroadcasters = Map(game);
                broadcasters.AddRange(gameBroadcasters);
            }

            return broadcasters;
        }
    }
}