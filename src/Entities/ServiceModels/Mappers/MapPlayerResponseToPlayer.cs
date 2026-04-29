using System.Text.Json.Nodes;
using Entities.Models;

namespace Entities.ServiceModels.Mappers;

public static class MapPlayerResponseToPlayer
{
    public static Player Map(JsonNode? playerResponse, int recentGameTeamId)
    {
        var playerDraftDetails = GetPlayerDraftDetails(playerResponse!["draftDetails"]);
        var currentTeamId = recentGameTeamId;
        var isActive = playerResponse["isActive"]!.GetValue<bool>();
        var birthStateProvince = playerResponse["birthStateProvince"] == null ? string.Empty
                                    : playerResponse["birthStateProvince"]!["default"]!.GetValue<string>();
        var birthCity = playerResponse["birthCity"] == null ? string.Empty
                                    : playerResponse["birthCity"]!["default"]!.GetValue<string>();
        return new Player()
        {
            Id = playerResponse["playerId"]!.GetValue<int>(),
            FirstName = playerResponse["firstName"]!["default"]!.GetValue<string>(),
            LastName = playerResponse["lastName"]!["default"]!.GetValue<string>(),
            IsActive = isActive,
            CurrentTeamId = currentTeamId,
            HeadShot = playerResponse["headshot"]!.GetValue<string>(),
            HeroImage = playerResponse["heroImage"]!.GetValue<string>(),
            HeightInInches = playerResponse["heightInInches"]?.GetValue<int>() ?? 0,
            WeightInPounds = playerResponse["weightInPounds"]?.GetValue<int>() ?? 0,
            BirthDate = DateTime.Parse(playerResponse["birthDate"]!.GetValue<string>()),
            BirthCity = birthCity,
            BirthStateProvince = birthStateProvince,
            BirthCountry = playerResponse["birthCountry"]!.GetValue<string>(),
            IsInTopOneHundredAllTime = playerResponse["inTop100AllTime"]!.GetValue<int>() != 0,
            IsInHallOfFame = playerResponse["inHHOF"]!.GetValue<int>() != 0,
            ShopLink = playerResponse["shopLink"]!.GetValue<string>(),
            TwitterLink = playerResponse["twitterLink"]!.GetValue<string>(),
            WatchLink = playerResponse["watchLink"]!.GetValue<string>(),
            PlayerSlug = playerResponse["playerSlug"]!.GetValue<string>(),
            PlayerDraftDetails = playerDraftDetails
        };
    }

    private static PlayerDraftDetails? GetPlayerDraftDetails(JsonNode? playerDraftResponse)
    {
        if (playerDraftResponse == null)
            return null;

        return new PlayerDraftDetails()
        {
            Year = playerDraftResponse["year"]!.GetValue<int>(),
            TeamAbbrev = playerDraftResponse["teamAbbrev"]!.GetValue<string>(),
            Round = playerDraftResponse["round"]!.GetValue<int>(),
            PickInRound = playerDraftResponse["pickInRound"]!.GetValue<int>(),
            OverallPick = playerDraftResponse["overallPick"]!.GetValue<int>()
        };
    }
}