using System.Text.Json.Nodes;
using Entities.Models;
using Entities.Types;
using Entities.Types.Mappers;

namespace Entities.ServiceModels.Mappers;

public static class MapGamePlayerStatsResponseToGamePlayerStats
{
    public static GameRosterStats Map(JsonNode? gameSummaryResponse, JsonNode? gamePlayerStatResponse)
    {
        var homeTeamId = gameSummaryResponse!["homeTeam"]!["id"]!.GetValue<int>();
        var awayTeamId = gameSummaryResponse["awayTeam"]!["id"]!.GetValue<int>();

        var homeCoachName = gamePlayerStatResponse!["gameInfo"]!["homeTeam"]!["headCoach"] != null
            ? gamePlayerStatResponse["gameInfo"]!["homeTeam"]!["headCoach"]!["default"]!.GetValue<string>()
            : "Unknown";
        var awayCoachName = gamePlayerStatResponse["gameInfo"]!["awayTeam"]!["headCoach"] != null
            ? gamePlayerStatResponse["gameInfo"]!["awayTeam"]!["headCoach"]!["default"]!.GetValue<string>()
            : "Unknown";

        var gameRosterStats = new GameRosterStats()
        {
            HomeTeamCoach = new Coach() { Name = homeCoachName },
            AwayTeamCoach = new Coach() { Name = awayCoachName },
            HomeTeamForwards = GetGameSkaters(gameSummaryResponse["playerByGameStats"]!["homeTeam"]!["forwards"]!, homeTeamId),
            AwayTeamForwards = GetGameSkaters(gameSummaryResponse["playerByGameStats"]!["awayTeam"]!["forwards"]!, awayTeamId),
            HomeTeamDefensemen = GetGameSkaters(gameSummaryResponse["playerByGameStats"]!["homeTeam"]!["defense"]!, homeTeamId),
            AwayTeamDefensemen = GetGameSkaters(gameSummaryResponse["playerByGameStats"]!["awayTeam"]!["defense"]!, awayTeamId),
            HomeTeamGoalies = GetGameGoalies(gameSummaryResponse["playerByGameStats"]!["homeTeam"]!["goalies"]!, homeTeamId),
            AwayTeamGoalies = GetGameGoalies(gameSummaryResponse["playerByGameStats"]!["awayTeam"]!["goalies"]!, awayTeamId)
        };

        BuildOfficials(gameRosterStats, gamePlayerStatResponse);

        return gameRosterStats;
    }

    private static IEnumerable<IGamePlayerStats> GetGameGoalies(JsonNode goalies, int homeTeamId)
    {
        var gameGoalies = new List<IGamePlayerStats>();
        foreach (var goalie in goalies.AsArray())
        {
            var evenStrengthShotsSaved = goalie!["evenStrengthShotsAgainst"] == null ? 0 :
                int.Parse(new string(goalie["evenStrengthShotsAgainst"]!.GetValue<string>().TakeWhile(Char.IsDigit).ToArray()));
            var powerPlayShotsSaved = goalie["powerPlayShotsAgainst"] == null ? 0 :
                int.Parse(new string(goalie["powerPlayShotsAgainst"]!.GetValue<string>().TakeWhile(Char.IsDigit).ToArray()));
            var shortHandedShotsSaved = goalie["shortHandedShotsAgainst"] == null ? 0 :
                int.Parse(new string(goalie["shortHandedShotsAgainst"]!.GetValue<string>().TakeWhile(Char.IsDigit).ToArray()));
            var evenStrengthGoalsAllowed = goalie["evenStrengthGoalsAgainst"] == null ? 0 : goalie["evenStrengthGoalsAgainst"]!.GetValue<int>();
            var powerPlayGoalsAllowed = goalie["powerPlayGoalsAgainst"] == null ? 0 : goalie["powerPlayGoalsAgainst"]!.GetValue<int>();
            var shortHandedGoalsAllowed = goalie["shortHandedGoalsAgainst"] == null ? 0 : goalie["shortHandedGoalsAgainst"]!.GetValue<int>();
            var timeOnIceSeconds = goalie["toi"] == null ? 0 : goalie["toi"]!.GetValue<string>().ParseIceTimeToSeconds();

            var goalieStats = new GameGoalieStats()
            {
                PlayerId = goalie["playerId"]!.GetValue<int>(),
                TeamId = homeTeamId,
                EvenStrengthShotsSaved = evenStrengthShotsSaved,
                PowerPlayShotsSaved = powerPlayShotsSaved,
                ShortHandedShotsSaved = shortHandedShotsSaved,
                EvenStrengthGoalsAllowed = evenStrengthGoalsAllowed,
                PowerPlayGoalsAllowed = powerPlayGoalsAllowed,
                ShortHandedGoalsAllowed = shortHandedGoalsAllowed,
                TimeOnIceSeconds = timeOnIceSeconds,
                IsStarter = goalie["starter"]!.GetValue<bool>(),
                Position = POSITION.Goalie
            };
            gameGoalies.Add(goalieStats);
        }

        return gameGoalies;
    }

    private static IEnumerable<IGamePlayerStats> GetGameSkaters(JsonNode players, int teamId)
    {
        var gamePlayers = new List<IGamePlayerStats>();
        foreach (var player in players.AsArray())
        {
            var playerStats = new GameSkaterStats()
            {
                PlayerId = player!["playerId"]!.GetValue<int>(),
                TeamId = teamId,
                Goals = player["goals"]!.GetValue<int>(),
                Assists = player["assists"]!.GetValue<int>(),
                ShotsOnGoal = player["sog"]!.GetValue<int>(),
                BlockedShots = player["blockedShots"]!.GetValue<int>(),
                PenaltyMinutes = player["pim"]!.GetValue<int>(),
                PowerPlayGoals = player["powerPlayGoals"]!.GetValue<int>(),
                PlusMinus = player["plusMinus"]!.GetValue<int>(),
                FaceOffWinningPctg = player["faceoffWinningPctg"]!.GetValue<double>(),
                Hits = player["hits"]!.GetValue<int>(),
                Giveaways = player["giveaways"]!.GetValue<int>(),
                Takeaways = player["takeaways"]!.GetValue<int>(),
                TimeOnIceSeconds = player["toi"]!.GetValue<string>().ParseIceTimeToSeconds(),
                Position = MapPositionStrToPosition.Map(player["position"]!.GetValue<string>())
            };
            gamePlayers.Add(playerStats);
        }

        return gamePlayers;
    }

    private static void BuildOfficials(GameRosterStats gameRosterStats, JsonNode gamePlayerStatResponse)
    {
        var referees = new List<IOfficial>();
        foreach (var referee in gamePlayerStatResponse["gameInfo"]!["referees"]!.AsArray())
        {
            referees.Add(new Referee()
            {
                Name = referee!["default"]!.GetValue<string>()
            });
        }
        gameRosterStats.Referees = referees;

        var linesmen = new List<IOfficial>();
        foreach (var linesman in gamePlayerStatResponse["gameInfo"]!["linesmen"]!.AsArray())
        {
            linesmen.Add(new Linesman()
            {
                Name = linesman!["default"]!.GetValue<string>()
            });
        }
        gameRosterStats.Linesmen = linesmen;
    }
}
