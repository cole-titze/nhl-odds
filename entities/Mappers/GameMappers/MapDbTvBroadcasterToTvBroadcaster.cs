using Entities.DbModels;
using Entities.Models;

namespace Entities.Mappers.GameMappers;

public static class MapDbTvBroadcasterToTvBroadcaster
{
    public static TvBroadcaster Map(DbTvBroadcaster dbTvBroadcaster)
    {
        return new TvBroadcaster()
        {
            Id = dbTvBroadcaster.Id,
            NetworkName = dbTvBroadcaster.NetworkName,
            MarketAbbreviation = dbTvBroadcaster.MarketAbbreviation,
            SequenceNumber = dbTvBroadcaster.SequenceNumber,
            CountryCode = dbTvBroadcaster.CountryCode,
        };
    }

    public static IEnumerable<TvBroadcaster> MapList(IEnumerable<DbTvBroadcaster> dbGameTvBroadcasters)
    {
        var gameList = new List<TvBroadcaster>();
        foreach (var dbGameTvBroadcaster in dbGameTvBroadcasters)
        {
            gameList.Add(Map(dbGameTvBroadcaster));
        }

        return gameList;
    }
}

