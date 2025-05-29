using Entities.DbModels;

namespace Entities.Models
{
    public class Roster
	{
		public List<DbGameSkaterStats> homeTeamSkaters { get; set; } = new List<DbGameSkaterStats>();
        public List<DbGameGoalieStats> homeTeamGoalies { get; set; } = new List<DbGameGoalieStats>();
		public List<DbGameSkaterStats> awayTeam { get; set; } = new List<DbGameSkaterStats>();
		public List<DbGameGoalieStats> awayTeamGoalies { get; set; } = new List<DbGameGoalieStats>();
    }
}

