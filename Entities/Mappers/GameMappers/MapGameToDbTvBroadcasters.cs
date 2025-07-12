using Entities.DbModels;
using Entities.Models;

namespace Entities.Mappers.GameMappers;

public static class MapGameToDbTvBroadcasters
{
    public static IEnumerable<DbTvBroadcaster> Map(Game game)
    {
        if (game.ExtendedInfo == null || game.ExtendedInfo.TvBroadcasters == null)
            throw new ArgumentNullException(nameof(game.ExtendedInfo), "Game extended info or TV broadcasters cannot be null.");

        var broadcasters = new List<DbTvBroadcaster>();
        foreach (var broadcaster in game.ExtendedInfo.TvBroadcasters)
        {
            var dbBroadcaster = new DbTvBroadcaster()
            {
                Id = broadcaster.Id,
                NetworkName = broadcaster.NetworkName,
                MarketAbbreviation = broadcaster.MarketAbbreviation,
                SequenceNumber = broadcaster.SequenceNumber,
                CountryCode = broadcaster.CountryCode
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