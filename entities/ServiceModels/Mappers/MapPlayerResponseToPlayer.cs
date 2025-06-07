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
				playerSlug = (string)playerResponse.playerSlug
			};
		}
	}
}
