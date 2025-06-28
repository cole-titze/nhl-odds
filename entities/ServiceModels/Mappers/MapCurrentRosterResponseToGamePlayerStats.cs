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
        public static GameRosterStats Map(dynamic homeRosterResponse, dynamic awayRosterResponse, int homeTeamId, int awayTeamId)
        {
            var gameRosterStats = new GameRosterStats()
            {
                homeTeamForwards = GetTeamForwards(homeRosterResponse, homeTeamId),
                homeTeamDefensemen = GetTeamDefensemen(homeRosterResponse, homeTeamId),
                homeTeamGoalies = GetTeamGoalies(homeRosterResponse, homeTeamId),
                awayTeamForwards = GetTeamForwards(awayRosterResponse, awayTeamId),
                awayTeamDefensemen = GetTeamDefensemen(awayRosterResponse, awayTeamId),
                awayTeamGoalies = GetTeamGoalies(awayRosterResponse, awayTeamId),
            };

            return gameRosterStats;
        }
        /// <summary>
        /// Gets the goalies for a team from the roster response.
        /// </summary>
        /// <param name="teamRosterResponse">The roster response from the NHL api</param>
        /// <param name="teamId">The team id</param>
        /// <returns>List of goalie stats for the game</returns>
        private static IEnumerable<IGamePlayerStats> GetTeamGoalies(dynamic teamRosterResponse, int teamId)
        {
            var gameGoalieStats = new List<IGamePlayerStats>();
            foreach (dynamic forwardsResponse in teamRosterResponse.goalies)
            {
                var gamePlayer = GetGoalie(forwardsResponse, teamId);
                gameGoalieStats.Add(gamePlayer);
            }

            return gameGoalieStats;
        }
        /// <summary>
        /// Gets the defensemen for a team from the roster response.
        /// </summary>
        /// <param name="teamRosterResponse">The roster response from the nhl api</param>
        /// <param name="teamId">The team id</param>
        /// <returns>List of defensemen stats for the game</returns>
        private static IEnumerable<IGamePlayerStats> GetTeamDefensemen(dynamic teamRosterResponse, int teamId)
        {
            var gameDefensemenStats = new List<IGamePlayerStats>();
            foreach (dynamic forwardsResponse in teamRosterResponse.defensemen)
            {
                var gamePlayer = GetSkater(forwardsResponse, teamId);
                gameDefensemenStats.Add(gamePlayer);
            }

            return gameDefensemenStats;
        }
        /// <summary>
        /// Gets the forwards for a team from the roster response.
        /// </summary>
        /// <param name="teamRosterResponse">The roster response from the nhl api</param>
        /// <param name="teamId">The team id</param>
        /// <returns>List of forwards stats for the game</returns>
        private static IEnumerable<IGamePlayerStats> GetTeamForwards(dynamic teamRosterResponse, int teamId)
        {
            var gameForwardsStats = new List<IGamePlayerStats>();
            foreach (dynamic forwardsResponse in teamRosterResponse.forwards)
            {
                var gamePlayer = GetSkater(forwardsResponse, teamId);
                gameForwardsStats.Add(gamePlayer);
            }

            return gameForwardsStats;
        }

        /// <summary>
        /// Creates a GameGoalieStats object from the roster and player response.
        /// </summary>
        /// <param name="goalieResponse">Nhl response for a goalie</param>
        /// <param name="teamId">The team id</param>
        /// <returns>The game goalie stats</returns>
        private static IGamePlayerStats GetGoalie(dynamic goalieResponse, int teamId)
        {
            return new GameGoalieStats()
            {
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
        /// <param name="teamId">The team id</param>
        /// <returns>The empty game skater stats</returns>
        private static GameSkaterStats GetSkater(dynamic skaterResponse, int teamId)
        {
            return new GameSkaterStats()
            {
                playerId = (int)skaterResponse.id,
                TeamId = teamId,
                Goals = 0,
                Assists = 0,
                PlusMinus = 0,
                PenaltyMinutes = 0,
                Hits = 0,
                PowerPlayGoals = 0,
                ShotsOnGoal = 0,
                FaceOffWinningPctg = 0.0,
                BlockedShots = 0,
                Giveaways = 0,
                Takeaways = 0,
                TimeOnIceSeconds = 0,
                Position = MapPositionStrToPosition.Map((string)skaterResponse.positionCode),
            };
        }
    }
}