using Entities.Models.Web;
using Entities.Types;
using Entities.ViewModels;

namespace WebApi.Mappers;

public static class GameOddsToViewModelsMapper
{
    public static IEnumerable<GameOddsVM> Map(IEnumerable<GameOdds> games)
    {
        var viewModelGames = new List<GameOddsVM>();
        foreach (var gameOdds in games)
        {
            var awayTeam = new MatchupTeamVM
            {
                id = gameOdds.game.awayTeam.id,
                locationName = gameOdds.game.awayTeam.locationName,
                teamName = gameOdds.game.awayTeam.teamName,
                logoUri = gameOdds.game.awayTeam.logoUri,
                modelOdds = gameOdds.modelAwayOdds,
                goals = gameOdds.game.awayGoals,
                team = Winner.AWAY
            };
            var homeTeam = new MatchupTeamVM
            {
                id = gameOdds.game.homeTeam.id,
                locationName = gameOdds.game.homeTeam.locationName,
                teamName = gameOdds.game.homeTeam.teamName,
                logoUri = gameOdds.game.homeTeam.logoUri,
                modelOdds = gameOdds.modelHomeOdds,
                goals = gameOdds.game.homeGoals,
                team = Winner.HOME
            };
            var viewModelGame = new GameOddsVM
            {
                id = gameOdds.game.id,
                gameDate = gameOdds.game.gameDate,
                awayTeam = awayTeam,
                homeTeam = homeTeam,
                winner = gameOdds.game.winner,
                hasBeenPlayed = gameOdds.game.hasBeenPlayed,
                logLoss = gameOdds.logLoss
            };
            viewModelGames.Add(viewModelGame);
        }
        return viewModelGames;
    }
}
