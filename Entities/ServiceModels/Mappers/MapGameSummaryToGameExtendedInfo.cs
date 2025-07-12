using Entities.Models;

namespace Entities.ServiceModels.Mappers;

public static class MapGameSummaryToGameExtendedInfo
{
    public static GameExtendedInfo Map(dynamic messageGameSummary)
    {
        var tvBroadcasters = new List<TvBroadcaster>();
        foreach (var broadcaster in messageGameSummary.tvBroadcasts)
        {
            tvBroadcasters.Add(new TvBroadcaster()
            {
                Id = (int)broadcaster.id,
                MarketAbbreviation = (string)broadcaster.market,
                NetworkName = (string)broadcaster.network,
                CountryCode = (string)broadcaster.countryCode,
                SequenceNumber = (int)broadcaster.sequenceNumber,
            });
        }

        return new GameExtendedInfo()
        {
            TvBroadcasters = tvBroadcasters,
            VenueName = (string)messageGameSummary.venue.@default,
            VenueLocation = (string)messageGameSummary.venueLocation.@default,
        };
    }
}