using Entities.Models.Web;
using Entities.Types;
using Entities.ViewModels;

namespace WebApi.Mappers;

public static class TeamToTeamVmMapper
{
    public static TeamVM Map(TeamStats teamStats)
    {
        return new TeamVM
        {
            id = teamStats.team.id,
            locationName = teamStats.team.locationName,
            teamName = teamStats.team.teamName,
            logoUri = teamStats.team.logoUri,
            modelLogLoss = GetAverageLogLoss(teamStats.gameOdds),
            totalGameCount = teamStats.gameOdds.Count(x => x.game.hasBeenPlayed),
            seasonWins = GetWins(teamStats.team.id, teamStats.gameOdds),
            seasonLosses = GetLosses(teamStats.team.id, teamStats.gameOdds),
            totalModelAccurateGameCount = GetCorrectModelPredictionCount(teamStats.gameOdds),
            gameOddsVM = GameOddsToViewModelsMapper.Map(teamStats.gameOdds),
        };
    }

    private static int GetWins(int teamId, IEnumerable<GameOdds> gameOdds)
    {
        return gameOdds.Sum(g => g.game.IsWin(teamId));
    }

    private static int GetLosses(int teamId, IEnumerable<GameOdds> gameOdds)
    {
        return gameOdds.Sum(g => g.game.IsLoss(teamId));
    }

    private static double GetAverageLogLoss(IEnumerable<GameOdds> gameOdds)
    {
        var playedWithLogLoss = gameOdds.Where(g => g.game.hasBeenPlayed && g.logLoss > 0).ToList();
        if (playedWithLogLoss.Count == 0)
            return 0;
        return playedWithLogLoss.Average(g => g.logLoss);
    }

    private static int GetCorrectModelPredictionCount(IEnumerable<GameOdds> gameOdds)
    {
        int correctCount = 0;
        foreach (var gameOdd in gameOdds)
        {
            correctCount += IsCorrectlyPredicted(gameOdd.game, gameOdd.modelHomeOdds, gameOdd.modelAwayOdds);
        }
        return correctCount;
    }

    private static int IsCorrectlyPredicted(Game game, double homeOdds, double awayOdds)
    {
        if (!game.hasBeenPlayed)
            return 0;

        if (homeOdds > .5 && game.winner == Winner.HOME)
            return 1;
        else if (awayOdds > .5 && game.winner == Winner.AWAY)
            return 1;

        return 0;
    }
}
