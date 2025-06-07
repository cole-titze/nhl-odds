namespace Entities.Models
{
	public class Roster
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
    }
}

