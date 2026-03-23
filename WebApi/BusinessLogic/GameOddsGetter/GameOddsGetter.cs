using DatabaseAccess.WebGameOddsRepository;
using Entities.Models.Web;
using Entities.Types;
using Entities.ViewModels;
using WebApi.Mappers;

namespace WebApi.BusinessLogic.GameOddsGetter;

public class GameOddsGetter : IGameOddsGetter
{
    private readonly IGameOddsRepository _gameOddsRepository;

    public GameOddsGetter(IGameOddsRepository predictedGameRepository)
    {
        _gameOddsRepository = predictedGameRepository;
    }

    public async Task<IEnumerable<GameOddsVM>> GetGameOddsInDateRange(DateRange dateRange, int seasonStartYear)
    {
        var gameOdds = await _gameOddsRepository.GetGameOddsInDateRange(dateRange, seasonStartYear);
        return GameOddsToViewModelsMapper.Map(gameOdds);
    }

    public async Task<IEnumerable<TeamStats>> BuildAllTeamsGameOdds(IEnumerable<TeamStats> teams, int seasonStartYear)
    {
        var allGameOdds = await _gameOddsRepository.GetAllGameOddsForSeason(seasonStartYear);

        foreach (var team in teams)
        {
            var teamId = team.Team.Id;
            team.GameOdds = allGameOdds
                .Where(g => g.Game.HomeTeam.Id == teamId || g.Game.AwayTeam.Id == teamId)
                .ToList();
        }

        return teams;
    }

    public async Task<TeamStats> BuildTeamGameOdds(TeamStats team, int seasonStartYear)
    {
        team.GameOdds = await _gameOddsRepository.GetTeamGameOdds(team.Team.Id, seasonStartYear);
        return team;
    }
}
