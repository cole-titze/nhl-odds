using Entities.DbModels;
using Entities.Models;

namespace Entities.Mappers.GameMappers;

public static class MapGameToDbGameTvBroadcasters
{
    public static IEnumerable<DbGameTvBroadcaster> Map(Game game)
    {
        if (game.ExtendedInfo == null || game.ExtendedInfo.TvBroadcasters == null)
            throw new ArgumentNullException(nameof(game.ExtendedInfo), "Game extended info or TV broadcasters cannot be null.");

        var broadcasters = new List<DbGameTvBroadcaster>();
        foreach (var broadcaster in game.ExtendedInfo.TvBroadcasters)
        {
            var dbBroadcaster = new DbGameTvBroadcaster()
            {
                GameId = game.Id,
                BroadcasterId = broadcaster.Id,
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