using Entities.Models;
using Entities.Types;
using Entities.Types.Mappers;

namespace Entities.ServiceModels.Mappers
{
    public static class MapGamePlayerStatsResponseToGamePlayerStats
    {
        /// <summary>
        /// Builds a player stats object
        /// Example response:
		/// https://api-web.nhle.com/v1/gamecenter/2023020204/right-rail
        /// </summary>
        /// <param name="playerStatResponse">Player stat response</param>
        /// <returns>Player stats object</returns>
		public static GameRosterStats Map(dynamic gameSummaryResponse, dynamic gamePlayerStatResponse)
        {
            var gameId = (int)gameSummaryResponse.id;
            var homeTeamId = (int)gameSummaryResponse.homeTeam.id;
            var awayTeamId = (int)gameSummaryResponse.awayTeam.id;

            var gameRosterStats = new GameRosterStats()
            {
                homeTeamCoach = new Coach() { name = (string)gamePlayerStatResponse.gameInfo.homeTeam.headCoach.@default },
                awayTeamCoach = new Coach() { name = (string)gamePlayerStatResponse.gameInfo.awayTeam.headCoach.@default },
                homeTeamForwards = GetGameSkaters(gameSummaryResponse.playerByGameStats.homeTeam.forwards, gameId, homeTeamId),
                awayTeamForwards = GetGameSkaters(gameSummaryResponse.playerByGameStats.awayTeam.forwards, gameId, awayTeamId),
                homeTeamDefensemen = GetGameSkaters(gameSummaryResponse.playerByGameStats.homeTeam.defense, gameId, homeTeamId).
                awayTeamDefensemen = GetGameSkaters(gameSummaryResponse.playerByGameStats.awayTeam.defense, gameId, awayTeamId),
                homeTeamGoalies = GetGameGoalies(gameSummaryResponse.playerByGameStats.homeTeam.goalies, gameId, homeTeamId),
                awayTeamGoalies = GetGameGoalies(gameSummaryResponse.playerByGameStats.awayTeam.goalies, gameId, awayTeamId)
            };

            BuildOfficials(gameRosterStats, gamePlayerStatResponse);

            return gameRosterStats;
        }

        private static IEnumerable<IGamePlayerStats> GetGameGoalies(dynamic goalies, int gameId, int homeTeamId)
        {
            var gameGoalies = new List<IGamePlayerStats>();
            foreach (var goalie in goalies)
            {

                var goalieStats = new GameGoalieStats()
                {
                    playerId = (int)goalie.playerId,
                    gameId = gameId,
                    teamId = homeTeamId,
                    evenStrengthShotsSaved = int.Parse(new string(((string)goalie.evenStrengthShotsAgainst).TakeWhile(Char.IsDigit).ToArray())),
                    powerPlayShotsSaved = int.Parse(new string(((string)goalie.powerPlayShotsAgainst).TakeWhile(Char.IsDigit).ToArray())),
                    shortHandedShotsSaved = int.Parse(new string(((string)goalie.shortHandedShotsAgainst).TakeWhile(Char.IsDigit).ToArray())),
                    evenStrengthGoalsAllowed = (int)goalie.evenStrengthGoalsAgainst,
                    powerPlayGoalsAllowed = (int)goalie.powerPlayGoalsAgainst,
                    shortHandedGoalsAllowed = (int)goalie.shortHandedGoalsAgainst,
                    timeOnIceSeconds = ((string)goalie.toi).ParseToiToSeconds(),
                    isStarter = (bool)goalie.starter,
                    position = POSITION.Goalie
                };
                gameGoalies.Add(goalieStats);
            }

            return gameGoalies;
        }

        private static IEnumerable<IGamePlayerStats> GetGameSkaters(dynamic players, int gameId, int teamId)
        {
            var gamePlayers = new List<IGamePlayerStats>();
            foreach (var player in players)
            {
                var playerStats = new GameSkaterStats()
                {
                    playerId = (int)player.playerId,
                    gameId = gameId,
                    teamId = teamId,
                    goals = (int)player.goals,
                    assists = (int)player.assists,
                    shotsOnGoal = (int)player.sog,
                    blockedShots = (int)player.blockedShots,
                    penaltyMinutes = (int)player.pim,
                    powerPlayGoals = (int)player.powerPlayGoals,
                    plusMinus = (int)player.plusMinus,
                    faceOffWinningPctg = (double)player.faceOffWinningPctg,
                    hits = (int)player.hits,
                    giveaways = (int)player.giveaways,
                    takeaways = (int)player.takeaways,
                    timeOnIceSeconds = ((string)player.toi).ParseToiToSeconds(),
                    position = MapPositionStrToPosition.Map((string)player.position)
                };
                gamePlayers.Add(playerStats);
            }

            return gamePlayers;
        }

        private static void BuildOfficials(GameRosterStats gameRosterStats, dynamic gamePlayerStatResponse)
        {
            // Add Referees
            var referees = new List<IOfficial>();
            foreach (var referee in gamePlayerStatResponse.gameInfo.referees)
            {
                referees.Add(new Referee()
                {
                    name = (string)referee.@default
                });
            }
            gameRosterStats.referees = referees;

            // Add linesmen
            var linesmen = new List<IOfficial>();
            foreach (var linesman in gamePlayerStatResponse.gameInfo.linesmen)
            {
                linesmen.Add(new Linesman()
                {
                    name = (string)linesman.@default
                });
            }
            gameRosterStats.linesmen = linesmen;
        }
    }
}
