using Entities.Models;

namespace Entities.ServiceModels.Mappers
{
    public static class MapGameSummaryToGameExtendedInfo
    {
        public static GameExtendedInfo Map(dynamic messageGameSummary)
        {
            var tvBroadcasters = new List<TvBroadcaster>();
            foreach(var broadcaster in messageGameSummary.tvBroadcasts)
            {
                tvBroadcasters.Add(new TvBroadcaster()
                {
                    id = (int)broadcaster.id,
                    marketAbbreviation = (string)broadcaster.market,
                    networkName = (string)broadcaster.network,
                    countryCode = (string)broadcaster.countryCode,
                    sequenceNumber = (int)broadcaster.sequenceNumber,
                });
            }

            return new GameExtendedInfo()
            {
                tvBroadcasters = tvBroadcasters,
                venueName = (string)messageGameSummary.venue.@default,
                venueLocation = (string)messageGameSummary.venueLocation.@default,
            };
        }
    }
}