using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using WebApi.BusinessLogic.TeamGetter;

namespace WebApi.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public class TeamController
{
    private readonly ITeamGetter _teamGetter;
    private readonly IMemoryCache _cache;

    public TeamController(ITeamGetter teamGetter, IMemoryCache cache)
    {
        _teamGetter = teamGetter;
        _cache = cache;
    }

    [HttpGet]
    public async Task<IResult> GetAllTeams(int seasonStartYear)
    {
        var cacheKey = $"AllTeams_{seasonStartYear}";
        if (_cache.TryGetValue(cacheKey, out object? cached))
            return Results.Ok(cached);

        var teamsVm = await _teamGetter.GetAllTeamsStats(seasonStartYear);

        _cache.Set(cacheKey, teamsVm, TimeSpan.FromMinutes(5));
        return Results.Ok(teamsVm);
    }

    [HttpGet]
    public async Task<IResult> GetTeam(int teamId, int seasonStartYear)
    {
        var teamVm = await _teamGetter.GetTeamStats(teamId, seasonStartYear);
        return Results.Ok(teamVm);
    }
}
