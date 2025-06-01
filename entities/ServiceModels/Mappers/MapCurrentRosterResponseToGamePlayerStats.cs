using Entities.Models;

namespace Entities.ServiceModels.Mappers
{
    public static class MapCurrentRosterResponseToGamePlayerStats
	{
		/// <summary>
		/// Maps the roster response to a list of player stats
		/// </summary>
		/// <param name="rosterResponse">Nhl response that contains a teams roster</param>
		/// <returns>List of player game stats</returns>
		public static IEnumerable<IGamePlayerStats> Map(dynamic rosterResponse)
		{

            return new List<IGamePlayerStats>()
            {
                new GameSkaterStats()
            };
		}
	}
}