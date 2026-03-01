using Entities.Models;
using Entities.Types;
using Entities.Types.Mappers;

namespace Entities.ServiceModels.Mappers;

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
        var homeTeamId = (int)gameSummaryResponse.homeTeam.id;
        var awayTeamId = (int)gameSummaryResponse.awayTeam.id;

        var homeCoachName = gamePlayerStatResponse.gameInfo.homeTeam.headCoach != null
            ? (string)gamePlayerStatResponse.gameInfo.homeTeam.headCoach.@default
            : "Unknown";
        var awayCoachName = gamePlayerStatResponse.gameInfo.awayTeam.headCoach != null
            ? (string)gamePlayerStatResponse.gameInfo.awayTeam.headCoach.@default
            : "Unknown";

        var gameRosterStats = new GameRosterStats()
        {
            HomeTeamCoach = new Coach() { Name = homeCoachName },
            AwayTeamCoach = new Coach() { Name = awayCoachName },
            HomeTeamForwards = GetGameSkaters(gameSummaryResponse.playerByGameStats.homeTeam.forwards, homeTeamId),
            AwayTeamForwards = GetGameSkaters(gameSummaryResponse.playerByGameStats.awayTeam.forwards, awayTeamId),
            HomeTeamDefensemen = GetGameSkaters(gameSummaryResponse.playerByGameStats.homeTeam.defense, homeTeamId),
            AwayTeamDefensemen = GetGameSkaters(gameSummaryResponse.playerByGameStats.awayTeam.defense, awayTeamId),
            HomeTeamGoalies = GetGameGoalies(gameSummaryResponse.playerByGameStats.homeTeam.goalies, homeTeamId),
            AwayTeamGoalies = GetGameGoalies(gameSummaryResponse.playerByGameStats.awayTeam.goalies, awayTeamId)
        };

        BuildOfficials(gameRosterStats, gamePlayerStatResponse);

        return gameRosterStats;
    }

    private static IEnumerable<IGamePlayerStats> GetGameGoalies(dynamic goalies, int homeTeamId)
    {
        var gameGoalies = new List<IGamePlayerStats>();
        foreach (var goalie in goalies)
        {
            var evenStrengthShotsSaved = goalie.evenStrengthShotsAgainst == null ? 0 :
                int.Parse(new string(((string)goalie.evenStrengthShotsAgainst).TakeWhile(Char.IsDigit).ToArray()));
            var powerPlayShotsSaved = goalie.powerPlayShotsAgainst == null ? 0 :
                int.Parse(new string(((string)goalie.powerPlayShotsAgainst).TakeWhile(Char.IsDigit).ToArray()));
            var shortHandedShotsSaved = goalie.shortHandedShotsAgainst == null ? 0 :
                int.Parse(new string(((string)goalie.shortHandedShotsAgainst).TakeWhile(Char.IsDigit).ToArray()));
            var evenStrengthGoalsAllowed = goalie.evenStrengthGoalsAgainst == null ? 0 : (int)goalie.evenStrengthGoalsAgainst;
            var powerPlayGoalsAllowed = goalie.powerPlayGoalsAgainst == null ? 0 : (int)goalie.powerPlayGoalsAgainst;
            var shortHandedGoalsAllowed = goalie.shortHandedGoalsAgainst == null ? 0 : (int)goalie.shortHandedGoalsAgainst;
            var timeOnIceSeconds = goalie.toi == null ? 0 : ((string)goalie.toi).ParseIceTimeToSeconds();

            var goalieStats = new GameGoalieStats()
            {
                PlayerId = (int)goalie.playerId,
                TeamId = homeTeamId,
                EvenStrengthShotsSaved = evenStrengthShotsSaved,
                PowerPlayShotsSaved = powerPlayShotsSaved,
                ShortHandedShotsSaved = shortHandedShotsSaved,
                EvenStrengthGoalsAllowed = evenStrengthGoalsAllowed,
                PowerPlayGoalsAllowed = powerPlayGoalsAllowed,
                ShortHandedGoalsAllowed = shortHandedGoalsAllowed,
                TimeOnIceSeconds = timeOnIceSeconds,
                IsStarter = (bool)goalie.starter,
                Position = POSITION.Goalie
            };
            gameGoalies.Add(goalieStats);
        }

        return gameGoalies;
    }

    private static IEnumerable<IGamePlayerStats> GetGameSkaters(dynamic players, int teamId)
    {
        var gamePlayers = new List<IGamePlayerStats>();
        foreach (var player in players)
        {
            var playerStats = new GameSkaterStats()
            {
                PlayerId = (int)player.playerId,
                TeamId = teamId,
                Goals = (int)player.goals,
                Assists = (int)player.assists,
                ShotsOnGoal = (int)player.sog,
                BlockedShots = (int)player.blockedShots,
                PenaltyMinutes = (int)player.pim,
                PowerPlayGoals = (int)player.powerPlayGoals,
                PlusMinus = (int)player.plusMinus,
                FaceOffWinningPctg = (double)player.faceoffWinningPctg,
                Hits = (int)player.hits,
                Giveaways = (int)player.giveaways,
                Takeaways = (int)player.takeaways,
                TimeOnIceSeconds = ((string)player.toi).ParseIceTimeToSeconds(),
                Position = MapPositionStrToPosition.Map((string)player.position)
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
                Name = (string)referee.@default
            });
        }
        gameRosterStats.Referees = referees;

        // Add linesmen
        var linesmen = new List<IOfficial>();
        foreach (var linesman in gamePlayerStatResponse.gameInfo.linesmen)
        {
            linesmen.Add(new Linesman()
            {
                Name = (string)linesman.@default
            });
        }
        gameRosterStats.Linesmen = linesmen;
    }
}
