using Entities.Models.Web;
using Entities.Types;
using Entities.ViewModels;

namespace WebApi.Mappers;

public static class TeamToTeamVmMapper
{
    private const string DraftKingsName = "DraftKings";

    public static TeamVM Map(TeamStats teamStats)
    {
        var dkStats = GetDraftKingsStats(teamStats.GameOdds);
        return new TeamVM
        {
            Id = teamStats.Team.Id,
            LocationName = teamStats.Team.LocationName,
            TeamName = teamStats.Team.TeamName,
            LogoUri = teamStats.Team.LogoUri,
            ModelLogLoss = GetAverageLogLoss(teamStats.GameOdds),
            TotalGameCount = teamStats.GameOdds.Count(x => x.Game.HasBeenPlayed),
            SeasonWins = GetWins(teamStats.Team.Id, teamStats.GameOdds),
            SeasonLosses = GetRegulationLosses(teamStats.Team.Id, teamStats.GameOdds),
            SeasonOvertimeLosses = GetOvertimeLosses(teamStats.Team.Id, teamStats.GameOdds),
            TotalModelAccurateGameCount = GetCorrectModelPredictionCount(teamStats.GameOdds),
            DraftKingsLogLoss = dkStats.LogLoss,
            DraftKingsAccurateGameCount = dkStats.AccurateCount,
            DraftKingsGameCount = dkStats.GameCount,
            GameOddsVM = GameOddsToViewModelsMapper.Map(teamStats.GameOdds),
        };
    }

    private static int GetWins(int teamId, IEnumerable<GameOdds> gameOdds)
    {
        return gameOdds.Sum(g => g.Game.IsWin(teamId));
    }

    private static int GetRegulationLosses(int teamId, IEnumerable<GameOdds> gameOdds)
    {
        return gameOdds.Sum(g => g.Game.IsLoss(teamId) - g.Game.IsOvertimeLoss(teamId));
    }

    private static int GetOvertimeLosses(int teamId, IEnumerable<GameOdds> gameOdds)
    {
        return gameOdds.Sum(g => g.Game.IsOvertimeLoss(teamId));
    }

    private static double GetAverageLogLoss(IEnumerable<GameOdds> gameOdds)
    {
        var playedWithLogLoss = gameOdds.Where(g => g.Game.HasBeenPlayed && g.LogLoss is double ll && ll > 0).ToList();
        if (playedWithLogLoss.Count == 0)
            return 0;
        return playedWithLogLoss.Average(g => g.LogLoss!.Value);
    }

    private static int GetCorrectModelPredictionCount(IEnumerable<GameOdds> gameOdds)
    {
        int correctCount = 0;
        foreach (var gameOdd in gameOdds)
        {
            if (gameOdd.ModelHomeOdds == null || gameOdd.ModelAwayOdds == null)
                continue;
            correctCount += IsCorrectlyPredicted(gameOdd.Game, gameOdd.ModelHomeOdds.Value, gameOdd.ModelAwayOdds.Value);
        }
        return correctCount;
    }

    private static int IsCorrectlyPredicted(Game game, double homeOdds, double awayOdds)
    {
        if (!game.HasBeenPlayed)
            return 0;

        if (homeOdds > .5 && game.Winner == Winner.HOME)
            return 1;
        else if (awayOdds > .5 && game.Winner == Winner.AWAY)
            return 1;

        return 0;
    }

    private static (double LogLoss, int AccurateCount, int GameCount) GetDraftKingsStats(IEnumerable<GameOdds> gameOdds)
    {
        double totalLogLoss = 0;
        int accurateCount = 0;
        int gameCount = 0;

        foreach (var g in gameOdds)
        {
            if (!g.Game.HasBeenPlayed)
                continue;

            var dk = g.BookmakerOdds.FirstOrDefault(b => b.BookmakerName == DraftKingsName);
            if (dk == null)
                continue;

            var logLoss = GameOdds.CalculateLogLoss(dk.HomeOdds, dk.AwayOdds, g.Game.Winner);
            if (logLoss < 0)
                continue;

            totalLogLoss += logLoss;
            accurateCount += IsCorrectlyPredicted(g.Game, dk.HomeOdds, dk.AwayOdds);
            gameCount++;
        }

        var avgLogLoss = gameCount > 0 ? totalLogLoss / gameCount : 0;
        return (avgLogLoss, accurateCount, gameCount);
    }
}