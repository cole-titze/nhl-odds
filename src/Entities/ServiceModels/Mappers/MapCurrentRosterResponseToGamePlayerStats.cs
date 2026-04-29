using System.Text.Json.Nodes;
using Entities.Models;
using Entities.Types.Mappers;

namespace Entities.ServiceModels.Mappers;

public static class MapCurrentRosterResponseToGamePlayerStats
{
    public static GameRosterStats Map(JsonNode? homeRosterResponse, JsonNode? awayRosterResponse, int homeTeamId, int awayTeamId)
    {
        var gameRosterStats = new GameRosterStats()
        {
            HomeTeamForwards = GetTeamForwards(homeRosterResponse, homeTeamId),
            HomeTeamDefensemen = GetTeamDefensemen(homeRosterResponse, homeTeamId),
            HomeTeamGoalies = GetTeamGoalies(homeRosterResponse, homeTeamId),
            AwayTeamForwards = GetTeamForwards(awayRosterResponse, awayTeamId),
            AwayTeamDefensemen = GetTeamDefensemen(awayRosterResponse, awayTeamId),
            AwayTeamGoalies = GetTeamGoalies(awayRosterResponse, awayTeamId),
        };

        return gameRosterStats;
    }

    private static IEnumerable<IGamePlayerStats> GetTeamGoalies(JsonNode? teamRosterResponse, int teamId)
    {
        var gameGoalieStats = new List<IGamePlayerStats>();
        foreach (var goalieResponse in teamRosterResponse!["goalies"]!.AsArray())
        {
            var gamePlayer = GetGoalie(goalieResponse!, teamId);
            gameGoalieStats.Add(gamePlayer);
        }

        return gameGoalieStats;
    }

    private static IEnumerable<IGamePlayerStats> GetTeamDefensemen(JsonNode? teamRosterResponse, int teamId)
    {
        var gameDefensemenStats = new List<IGamePlayerStats>();
        foreach (var defensemanResponse in teamRosterResponse!["defensemen"]!.AsArray())
        {
            var gamePlayer = GetSkater(defensemanResponse!, teamId);
            gameDefensemenStats.Add(gamePlayer);
        }

        return gameDefensemenStats;
    }

    private static IEnumerable<IGamePlayerStats> GetTeamForwards(JsonNode? teamRosterResponse, int teamId)
    {
        var gameForwardsStats = new List<IGamePlayerStats>();
        foreach (var forwardResponse in teamRosterResponse!["forwards"]!.AsArray())
        {
            var gamePlayer = GetSkater(forwardResponse!, teamId);
            gameForwardsStats.Add(gamePlayer);
        }

        return gameForwardsStats;
    }

    private static IGamePlayerStats GetGoalie(JsonNode goalieResponse, int teamId)
    {
        return new GameGoalieStats()
        {
            PlayerId = goalieResponse["id"]!.GetValue<int>(),
            TeamId = teamId,
            EvenStrengthShotsSaved = 0,
            PowerPlayShotsSaved = 0,
            EvenStrengthGoalsAllowed = 0,
            PowerPlayGoalsAllowed = 0,
            TimeOnIceSeconds = 0,
            IsStarter = false,
            Position = MapPositionStrToPosition.Map(goalieResponse["position"]!.GetValue<string>()),
        };
    }

    private static GameSkaterStats GetSkater(JsonNode skaterResponse, int teamId)
    {
        return new GameSkaterStats()
        {
            PlayerId = skaterResponse["id"]!.GetValue<int>(),
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
            Position = MapPositionStrToPosition.Map(skaterResponse["positionCode"]!.GetValue<string>()),
        };
    }
}