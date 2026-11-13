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
        var teamsVm = await _cache.GetOrSetAllTeamsAsync(seasonStartYear,
            () => _teamGetter.GetAllTeamsStats(seasonStartYear));
        return Results.Ok(teamsVm);
    }

    // The Teams page's data (see SeasonTeamsVM), built from the cached GetAllTeams response
    [HttpGet]
    public async Task<IResult> GetSeasonTeams(int seasonStartYear)
    {
        var seasonTeams = await _cache.GetOrSetAsync(ApiCache.SeasonTeamsKey(seasonStartYear), async () =>
        {
            var teamsVm = await _cache.GetOrSetAllTeamsAsync(seasonStartYear,
                () => _teamGetter.GetAllTeamsStats(seasonStartYear));
            return TeamsVmToSeasonTeamsVmMapper.Map(teamsVm);
        }, ApiCache.SizeOf, priority: ApiCache.SeasonPriority(seasonStartYear));
        return Results.Ok(seasonTeams);
    }

    // One of GetSeasonTeams' OtherBookmakers, for when it's picked on the Teams page's chart
    [HttpGet]
    public async Task<IResult> GetSeasonBookmakerOdds(int seasonStartYear, string bookmaker)
    {
        var teamsVm = await _cache.GetOrSetAllTeamsAsync(seasonStartYear,
            () => _teamGetter.GetAllTeamsStats(seasonStartYear));
        return Results.Ok(TeamsVmToSeasonTeamsVmMapper.MapBookmakerOdds(teamsVm, bookmaker));
    }

    [HttpGet]
    public async Task<IResult> GetTeam(int teamId, int seasonStartYear)
    {
        var teamVm = await _cache.GetOrSetAsync(ApiCache.TeamKey(teamId, seasonStartYear),
            () => _teamGetter.GetTeamStats(teamId, seasonStartYear), ApiCache.SizeOf, ApiCache.DateRangeLifetime);
        return teamVm is null ? Results.NotFound() : Results.Ok(teamVm);
    }
}