using Microsoft.AspNetCore.Mvc;
using WebApi.BusinessLogic.GameOddsGetter;
using WebApi.BusinessLogic.TeamGetter;
using WebApi.Mappers;

namespace WebApi.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public class TeamController
{
    private readonly ILogger<TeamController> _logger;
    private readonly ITeamGetter _teamGetter;
    private readonly IGameOddsGetter _gameOddsGetter;

    public TeamController(ILogger<TeamController> logger, ITeamGetter teamGetter, IGameOddsGetter gameOddsBL)
    {
        _logger = logger;
        _teamGetter = teamGetter;
        _gameOddsGetter = gameOddsBL;
    }

    [HttpGet]
    public async Task<IResult> GetAllTeams(int seasonStartYear)
    {
        var teams = await _teamGetter.GetAllTeamsStats(seasonStartYear);
        teams = await _gameOddsGetter.BuildTeamsGameOdds(teams, seasonStartYear);
        var teamsVm = TeamsToTeamsVmMapper.Map(teams);
        return Results.Ok(teamsVm);
    }

    [HttpGet]
    public async Task<IResult> GetTeam(int teamId, int seasonStartYear)
    {
        var team = await _teamGetter.GetTeamStats(teamId, seasonStartYear);
        team = await _gameOddsGetter.BuildTeamGameOdds(team, seasonStartYear);
        var teamVm = TeamToTeamVmMapper.Map(team);
        return Results.Ok(teamVm);
    }
}
