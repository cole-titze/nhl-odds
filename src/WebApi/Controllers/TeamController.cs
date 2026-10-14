using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using WebApi.BusinessLogic.TeamGetter;
using WebApi.Caching;
using WebApi.Mappers;

namespace WebApi.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public class TeamController
{
    private readonly ITeamGetter _teamGetter;
    private readonly IMemoryCache _cache;

    public TeamController(ITeamGetter teamGetter, [FromKeyedServices(ApiCache.ServiceKey)] IMemoryCache cache)
    {
        _teamGetter = teamGetter;
        _cache = cache;
    }

    [HttpGet]
    public async Task<IResult> GetAllTeams(int seasonStartYear)
    {
        var teamsVm = await _cache.GetOrSetAsync(ApiCache.AllTeamsKey(seasonStartYear),
            () => _teamGetter.GetAllTeamsStats(seasonStartYear), ApiCache.SizeOf);
        return Results.Ok(teamsVm);
    }

    // The Teams page's data (see SeasonTeamsVM), built from the cached GetAllTeams response
    [HttpGet]
    public async Task<IResult> GetSeasonTeams(int seasonStartYear)
    {
        var teamsVm = await _cache.GetOrSetAsync(ApiCache.AllTeamsKey(seasonStartYear),
            () => _teamGetter.GetAllTeamsStats(seasonStartYear), ApiCache.SizeOf);
        return Results.Ok(TeamsVmToSeasonTeamsVmMapper.Map(teamsVm));
    }

    [HttpGet]
    public async Task<IResult> GetTeam(int teamId, int seasonStartYear)
    {
        var teamVm = await _cache.GetOrSetAsync(ApiCache.TeamKey(teamId, seasonStartYear),
            () => _teamGetter.GetTeamStats(teamId, seasonStartYear), ApiCache.SizeOf, ApiCache.DateRangeLifetime);
        return Results.Ok(teamVm);
    }
}