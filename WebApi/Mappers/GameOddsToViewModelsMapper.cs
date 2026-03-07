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
                Id = gameOdds.Game.AwayTeam.Id,
                LocationName = gameOdds.Game.AwayTeam.LocationName,
                TeamName = gameOdds.Game.AwayTeam.TeamName,
                LogoUri = gameOdds.Game.AwayTeam.LogoUri,
                ModelOdds = gameOdds.ModelAwayOdds,
                Goals = gameOdds.Game.AwayGoals,
                Team = Winner.AWAY
            };
            var homeTeam = new MatchupTeamVM
            {
                Id = gameOdds.Game.HomeTeam.Id,
                LocationName = gameOdds.Game.HomeTeam.LocationName,
                TeamName = gameOdds.Game.HomeTeam.TeamName,
                LogoUri = gameOdds.Game.HomeTeam.LogoUri,
                ModelOdds = gameOdds.ModelHomeOdds,
                Goals = gameOdds.Game.HomeGoals,
                Team = Winner.HOME
            };
            var viewModelGame = new GameOddsVM
            {
                Id = gameOdds.Game.Id,
                GameDate = gameOdds.Game.GameDate,
                AwayTeam = awayTeam,
                HomeTeam = homeTeam,
                Winner = gameOdds.Game.Winner,
                HasBeenPlayed = gameOdds.Game.HasBeenPlayed,
                LogLoss = gameOdds.LogLoss
            };
            viewModelGames.Add(viewModelGame);
        }
        return viewModelGames;
    }
}
