using Entities.Models;
using Entities.Types.Mappers;

namespace Entities.ServiceModels.Mappers
{
    public static class MapCurrentRosterResponseToGamePlayerStats
	{
		/// <summary>
		/// Maps the roster response to a list of player stats
		/// Example call that could get mapped: 
		/// https://api-web.nhle.com/v1/roster/TOR/current
		/// </summary>
		/// <param name="rosterResponse">Nhl response that contains a teams roster</param>
		/// <param name="playerResponse">Player response in the roster response</param>
		/// <returns>List of player game stats</returns>
		public static IEnumerable<IGamePlayerStats> Map(dynamic rosterResponse, int gameId, int teamId)
		{
			var futureGamePlayerStats = new List<IGamePlayerStats>();
			// Get forwards
			foreach (dynamic forwardsResponse in rosterResponse.forwards)
			{
				var gamePlayer = GetSkater(forwardsResponse, gameId, teamId);
				futureGamePlayerStats.Add(gamePlayer);
			}

			// Get Defensemen
			foreach (dynamic forwardsResponse in rosterResponse.defensemen)
			{
				var gamePlayer = GetSkater(forwardsResponse, gameId, teamId);
				futureGamePlayerStats.Add(gamePlayer);
			}

			// Get Goalies
			foreach (dynamic goaliesResponse in rosterResponse.goalies)
			{
				var gamePlayer = GetGoalie(goaliesResponse, gameId, teamId);
				futureGamePlayerStats.Add(gamePlayer);
			}

			return futureGamePlayerStats;
		}
		/// <summary>
		/// Creates a GameGoalieStats object from the roster and player response.
		/// </summary>
		/// <param name="goalieResponse">Nhl response for a goalie</param>
		/// <param name="gameId">The game id</param>
		/// <param name="teamId">The team id</param>
		/// <returns>The game goalie stats</returns>
        private static IGamePlayerStats GetGoalie(dynamic goalieResponse, int gameId, int teamId)
        {
            return new GameGoalieStats()
			{
				gameId = gameId,
				playerId = (int)goalieResponse.id,
				teamId = teamId,
				evenStrengthShotsSaved = 0,
				powerPlayShotsSaved = 0,
				evenStrengthGoalsAllowed = 0,
				powerPlayGoalsAllowed = 0,
				timeOnIceSeconds = 0,
				isStarter = false,
				position = MapPositionStrToPosition.Map((string)goalieResponse.position),
			};
        }
		/// <summary>
		/// Creates a GameSkaterStats object from the roster and player response.
		/// </summary>
		/// <param name="skaterResponse">Response for a skater on a roster</param>
		/// <param name="gameId">The game id</param>
		/// <param name="teamId">The team id</param>
		/// <returns>The empty game skater stats</returns>
        private static GameSkaterStats GetSkater(dynamic skaterResponse, int gameId, int teamId)
        {
            return new GameSkaterStats()
            {
                gameId = gameId,
                playerId = (int)skaterResponse.id,
                teamId = teamId,
                goals = 0,
                assists = 0,
                plusMinus = 0,
                penaltyMinutes = 0,
                hits = 0,
                powerPlayGoals = 0,
                shotsOnGoal = 0,
                faceOffWinningPctg = 0.0,
                blockedShots = 0,
                giveaways = 0,
                takeaways = 0,
                timeOnIceSeconds = 0,
                position = MapPositionStrToPosition.Map((string)skaterResponse.positionCode),
            };
        }
    }
}