using DatabaseAccess.WebTeamRepository;
using Entities.ViewModels;
using WebApi.BusinessLogic.GameOddsGetter;
using WebApi.Mappers;

namespace WebApi.BusinessLogic.TeamGetter;

public class TeamGetter : ITeamGetter
{
    private readonly ITeamRepository _teamRepository;
    private readonly IGameOddsGetter _gameOddsGetter;

    public TeamGetter(ITeamRepository teamRepository, IGameOddsGetter gameOddsGetter)
    {
        _teamRepository = teamRepository;
        _gameOddsGetter = gameOddsGetter;
    }

    public async Task<TeamsVM> GetAllTeamsStats(int seasonStartYear)
    {
        var teams = await _teamRepository.GetAllTeams(seasonStartYear);
        teams = await _gameOddsGetter.BuildAllTeamsGameOdds(teams, seasonStartYear);
        return TeamsToTeamsVmMapper.Map(teams);
    }

    public async Task<TeamVM> GetTeamStats(int teamId, int seasonStartYear)
    {
        var team = await _teamRepository.GetTeam(teamId, seasonStartYear);
        team = await _gameOddsGetter.BuildTeamGameOdds(team, seasonStartYear);
        return TeamToTeamVmMapper.Map(team);
    }
}
