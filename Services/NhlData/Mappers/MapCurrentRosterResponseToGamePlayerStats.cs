using Entities.DbModels;
using Entities.Models;

namespace Services.NhlData.Mappers
{
    public static class MapCurrentRosterResponseToGamePlayerStats
	{
		/// <summary>
		/// Maps the roster response to a list of player stats
		/// </summary>
		/// <param name="rosterResponse">Nhl response that contains a teams roster</param>
		/// <returns>List of player game stats</returns>
		public static IEnumerable<IDbGamePlayerStats> Map(dynamic rosterResponse)
		{

            return new List<IDbGamePlayerStats>()
            {
                new DbGameSkaterStats()
            };
		}
	}
}