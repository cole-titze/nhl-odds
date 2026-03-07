using DatabaseAccess.WebGameOddsRepository;
using Entities.Models.Web;
using Entities.Types;

namespace WebApi.BusinessLogic.GameOddsGetter;

public class GameOddsGetter : IGameOddsGetter
{
    private readonly IGameOddsRepository _gameOddsRepository;

    public GameOddsGetter(IGameOddsRepository predictedGameRepository)
    {
        _gameOddsRepository = predictedGameRepository;
    }

    public async Task<IEnumerable<GameOdds>> GetGameOddsInDateRange(DateRange dateRange, int seasonStartYear)
    {
        return await _gameOddsRepository.GetGameOddsInDateRange(dateRange, seasonStartYear);
    }

    public async Task<IEnumerable<TeamStats>> BuildTeamsGameOdds(IEnumerable<TeamStats> teams, int seasonStartYear)
    {
        foreach (var team in teams)
        {
            await BuildTeamGameOdds(team, seasonStartYear);
        }

        return teams;
    }

    public async Task<TeamStats> BuildTeamGameOdds(TeamStats team, int seasonStartYear)
    {
        team.GameOdds = await GetTeamGameOdds(team.Team.Id, seasonStartYear);
        return team;
    }

    public async Task<IEnumerable<GameOdds>> GetTeamGameOdds(int teamId, int seasonStartYear)
    {
        return await _gameOddsRepository.GetTeamGameOdds(teamId, seasonStartYear);
    }
}
