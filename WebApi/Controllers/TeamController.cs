using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
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
    private readonly IMemoryCache _cache;

    public TeamController(ILogger<TeamController> logger, ITeamGetter teamGetter, IGameOddsGetter gameOddsBL, IMemoryCache cache)
    {
        _logger = logger;
        _teamGetter = teamGetter;
        _gameOddsGetter = gameOddsBL;
        _cache = cache;
    }

    [HttpGet]
    public async Task<IResult> GetAllTeams(int seasonStartYear)
    {
        var cacheKey = $"AllTeams_{seasonStartYear}";
        if (_cache.TryGetValue(cacheKey, out object? cached))
            return Results.Ok(cached);

        var teams = await _teamGetter.GetAllTeamsStats(seasonStartYear);
        teams = await _gameOddsGetter.BuildAllTeamsGameOdds(teams, seasonStartYear);
        var teamsVm = TeamsToTeamsVmMapper.Map(teams);

        _cache.Set(cacheKey, teamsVm, TimeSpan.FromMinutes(5));
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
