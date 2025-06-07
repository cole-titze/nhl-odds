namespace Entities.Models
{
	/// <summary>
	/// Represents the stats for a game roster, including teams, coaches, and players.
	/// </summary>
	public class GameRosterStats
	{
		public Coach homeTeamCoach { get; set; } = new Coach();
		public IEnumerable<IGamePlayerStats> homeTeamForwards { get; set; } = new List<IGamePlayerStats>();
		public IEnumerable<IGamePlayerStats> homeTeamDefensemen { get; set; } = new List<IGamePlayerStats>();
		public IEnumerable<IGamePlayerStats> homeTeamGoalies { get; set; } = new List<IGamePlayerStats>();
		public Coach awayTeamCoach { get; set; } = new Coach();
		public IEnumerable<IGamePlayerStats> awayTeamForwards { get; set; } = new List<IGamePlayerStats>();
		public IEnumerable<IGamePlayerStats> awayTeamDefensement { get; set; } = new List<IGamePlayerStats>();
		public IEnumerable<IGamePlayerStats> awayTeamGoalies { get; set; } = new List<IGamePlayerStats>();
		public IEnumerable<Referee> linesmen { get; set; } = new List<Referee>();

		/// <summary>
		/// Enumerates all players (forwards, defensemen, goalies) from both teams.
		/// </summary>
		/// <returns>An enumerable collection of all players in the game.</returns>
		public IEnumerable<IGamePlayerStats> AllPlayers
		{
			get
			{
				return homeTeamForwards
					.Concat(homeTeamDefensemen)
					.Concat(homeTeamGoalies)
					.Concat(awayTeamForwards)
					.Concat(awayTeamDefensement)
					.Concat(awayTeamGoalies);
			}
		}
	}
}

