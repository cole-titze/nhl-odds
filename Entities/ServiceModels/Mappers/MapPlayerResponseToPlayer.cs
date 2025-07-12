using Entities.Models;

namespace Entities.ServiceModels.Mappers;

public static class MapPlayerResponseToPlayer
{
    /// <summary>
    /// Maps the player response to a list of player ids
    /// Example response:
    /// https://api-web.nhle.com/v1/player/8478402/landing
    /// </summary>
    /// <param name="playerResponse">Nhl response that contains a teams roster</param>
    /// <param name="recentGameTeamId">The team id of the most recent game the player played in</param>
    /// <returns>The player object</returns>
    public static Player Map(dynamic playerResponse, int recentGameTeamId)
    {
        var playerDraftDetails = GetPlayerDraftDetails(playerResponse.draftDetails);
        var currentTeamId = recentGameTeamId;
        var isActive = (bool)playerResponse.isActive;
        var birthStateProvince = playerResponse.birthStateProvince == null ? string.Empty
                                    : (string)playerResponse.birthStateProvince.@default;
        return new Player()
        {
            Id = (int)playerResponse.playerId,
            FirstName = (string)playerResponse.firstName.@default,
            LastName = (string)playerResponse.lastName.@default,
            IsActive = isActive,
            CurrentTeamId = currentTeamId,
            HeadShot = (string)playerResponse.headshot,
            HeroImage = (string)playerResponse.heroImage,
            HeightInInches = (int)playerResponse.heightInInches,
            WeightInPounds = (int)playerResponse.weightInPounds,
            BirthDate = DateTime.Parse((string)playerResponse.birthDate),
            BirthCity = (string)playerResponse.birthCity.@default,
            BirthStateProvince = birthStateProvince,
            BirthCountry = playerResponse.birthCountry,
            IsInTopOneHundredAllTime = (bool)playerResponse.inTop100AllTime,
            IsInHallOfFame = (bool)playerResponse.inHHOF,
            ShopLink = (string)playerResponse.shopLink,
            TwitterLink = (string)playerResponse.twitterLink,
            WatchLink = (string)playerResponse.watchLink,
            PlayerSlug = (string)playerResponse.playerSlug,
            PlayerDraftDetails = playerDraftDetails
        };
    }

    /// <summary>
    /// Gets the player draft details from the player response
    /// </summary>
    /// <param name="playerDraftResponse">NHL response for a player draft details</param>
    /// <returns>The players draft details</returns>
    private static PlayerDraftDetails? GetPlayerDraftDetails(dynamic playerDraftResponse)
    {
        if (playerDraftResponse == null)
            return null;

        return new PlayerDraftDetails()
        {
            Year = (int)playerDraftResponse.year,
            TeamAbbrev = (string)playerDraftResponse.teamAbbrev,
            Round = (int)playerDraftResponse.round,
            PickInRound = (int)playerDraftResponse.pickInRound,
            OverallPick = (int)playerDraftResponse.overallPick
        };
    }
}
