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
            Id = teamStats.Team.Id,
            LocationName = teamStats.Team.LocationName,
            TeamName = teamStats.Team.TeamName,
            LogoUri = teamStats.Team.LogoUri,
            ModelLogLoss = GetAverageLogLoss(teamStats.GameOdds),
            TotalGameCount = teamStats.GameOdds.Count(x => x.Game.HasBeenPlayed),
            SeasonWins = GetWins(teamStats.Team.Id, teamStats.GameOdds),
            SeasonLosses = GetLosses(teamStats.Team.Id, teamStats.GameOdds),
            TotalModelAccurateGameCount = GetCorrectModelPredictionCount(teamStats.GameOdds),
            GameOddsVM = GameOddsToViewModelsMapper.Map(teamStats.GameOdds),
        };
    }

    private static int GetWins(int teamId, IEnumerable<GameOdds> gameOdds)
    {
        return gameOdds.Sum(g => g.Game.IsWin(teamId));
    }

    private static int GetLosses(int teamId, IEnumerable<GameOdds> gameOdds)
    {
        return gameOdds.Sum(g => g.Game.IsLoss(teamId));
    }

    private static double GetAverageLogLoss(IEnumerable<GameOdds> gameOdds)
    {
        var playedWithLogLoss = gameOdds.Where(g => g.Game.HasBeenPlayed && g.LogLoss > 0).ToList();
        if (playedWithLogLoss.Count == 0)
            return 0;
        return playedWithLogLoss.Average(g => g.LogLoss);
    }

    private static int GetCorrectModelPredictionCount(IEnumerable<GameOdds> gameOdds)
    {
        int correctCount = 0;
        foreach (var gameOdd in gameOdds)
        {
            correctCount += IsCorrectlyPredicted(gameOdd.Game, gameOdd.ModelHomeOdds, gameOdd.ModelAwayOdds);
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
}
