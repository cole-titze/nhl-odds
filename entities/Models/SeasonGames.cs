namespace Entities.Models
{
	public class SeasonGames
	{
		public readonly IDictionary<int, DateSortedTeamGames> GamesMap = new Dictionary<int, DateSortedTeamGames>();
		public SeasonGames(IEnumerable<Game> games)
		{
			foreach (var game in games)
			{
				GamesMap.TryAdd(game.homeTeamId, new DateSortedTeamGames(games, game.homeTeamId));
				GamesMap.TryAdd(game.awayTeamId, new DateSortedTeamGames(games, game.awayTeamId));
			}
		}
		/// <summary>
        /// Gets if the season start year is the current season or not
        /// </summary>
        /// <param name="seasonStartYear">Season to check</param>
        /// <param name="currentSeason">The current season start year</param>
        /// <returns>True if the season to check is the same as the current season, otherwise false</returns>
        public static bool IsCurrentSeason(int seasonStartYear, int currentSeason)
        {
            return seasonStartYear == currentSeason;
        }
	}
}

