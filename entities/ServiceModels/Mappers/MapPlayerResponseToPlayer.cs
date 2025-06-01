using Entities.Models;

namespace Entities.ServiceModels.Mappers
{
    public static class MapPlayerResponseToPlayer
	{
		/// <summary>
		/// Maps the player response to a list of player ids
		/// </summary>
		/// <param name="playerResponse">Nhl response that contains a teams roster</param>
		/// <returns></returns>
		public static Player Map(dynamic playerResponse)
		{

            return new Player();
		}
	}
}
