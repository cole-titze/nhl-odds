using System.Text.Json.Nodes;
using Entities.Models;

namespace Entities.ServiceModels.Mappers;

public static class MapGameSummaryToGameExtendedInfo
{
    public static GameExtendedInfo Map(JsonNode? messageGameSummary)
    {
        var tvBroadcasters = new List<TvBroadcaster>();
        foreach (var broadcaster in messageGameSummary!["tvBroadcasts"]!.AsArray())
        {
            tvBroadcasters.Add(new TvBroadcaster()
            {
                Id = broadcaster!["id"]!.GetValue<int>(),
                MarketAbbreviation = broadcaster["market"]!.GetValue<string>(),
                NetworkName = broadcaster["network"]!.GetValue<string>(),
                CountryCode = broadcaster["countryCode"]!.GetValue<string>(),
                SequenceNumber = broadcaster["sequenceNumber"]!.GetValue<int>(),
            });
        }

        return new GameExtendedInfo()
        {
            TvBroadcasters = tvBroadcasters,
            VenueName = messageGameSummary["venue"]!["default"]!.GetValue<string>(),
            VenueLocation = messageGameSummary["venueLocation"]!["default"]!.GetValue<string>(),
        };
    }
}