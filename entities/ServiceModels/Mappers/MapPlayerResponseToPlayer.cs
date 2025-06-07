using Entities.Models;

namespace Entities.ServiceModels.Mappers
{
    public static class MapPlayerResponseToPlayer
	{
		/// <summary>
		/// Maps the player response to a list of player ids
		/// Example response:
		/// https://api-web.nhle.com/v1/player/8478402/landing
		/// </summary>
		/// <param name="playerResponse">Nhl response that contains a teams roster</param>
		/// <returns></returns>
		public static Player Map(dynamic playerResponse)
		{
			var playerDraftDetails = GetPlayerDraftDetails(playerResponse.draftDetails);
            return new Player()
			{
				id = (int)playerResponse.playerId,
				firstName = (string)playerResponse.firstName.@default,
				lastName = (string)playerResponse.lastName.@default,
				isActive = (bool)playerResponse.isActive,
				currentTeamId = (int)playerResponse.currentTeamId,
				headShot = (string)playerResponse.headShot,
				heroImage = (string)playerResponse.heroImage,
				heightInInches = (int)playerResponse.heightInInches,
				weightInPounds = (int)playerResponse.weightInPounds,
				birthDate = DateTime.Parse(playerResponse.birthDate),
				birthCity = (string)playerResponse.birthCity.@default,
				birthStateProvince = (string)playerResponse.birthStateProvince.@default,
				isInTopOneHundredAllTime = (bool)playerResponse.isInTopOneHundredAllTime,
				isInHallOfFame = (bool)playerResponse.isInHallOfFame,
				shopLink = (string)playerResponse.shopLink,
				twitterLink = (string)playerResponse.twitterLink,
				watchLink = (string)playerResponse.watchLink,
				playerSlug = (string)playerResponse.playerSlug,
				playerDraftDetails = playerDraftDetails
			};
		}
		/// <summary>
		/// Gets the player draft details from the player response
		/// </summary>
		/// <param name="playerDraftResponse">NHL response for a player draft details</param>
		/// <returns>The players draft details</returns>
        private static PlayerDraftDetails GetPlayerDraftDetails(dynamic playerDraftResponse)
        {
            return new PlayerDraftDetails()
			{
				year = (int)playerDraftResponse.year,
				teamAbbrev = (string)playerDraftResponse.teamAbbrev,
				round = (int)playerDraftResponse.round,
				pickInRound = (int)playerDraftResponse.pickInRound,
				overallPick = (int)playerDraftResponse.overallPick
			};
        }
    }
}
