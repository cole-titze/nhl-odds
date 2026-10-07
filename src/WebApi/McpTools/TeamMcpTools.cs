using System.ComponentModel;
using Entities.ViewModels;
using Microsoft.Extensions.Caching.Memory;
using ModelContextProtocol.Server;
using WebApi.BusinessLogic.TeamGetter;
using WebApi.Caching;
using WebApi.Mappers;

namespace WebApi.McpTools;

[McpServerToolType]
public class TeamMcpTools(ITeamGetter teamGetter, [FromKeyedServices(ApiCache.ServiceKey)] IMemoryCache cache)
{
    [McpServerTool, Description("Get all NHL teams and their season stats, win/loss records, and model accuracy. Does not include games; use GetTeamStats for a team's games.")]
    public async Task<TeamsSummaryVM> GetAllTeams(
        [Description("The season start year (e.g. 2024 for the 2024-25 season)")] int seasonStartYear)
    {
        var teamsVm = await cache.GetOrSetAsync(ApiCache.AllTeamsKey(seasonStartYear),
            () => teamGetter.GetAllTeamsStats(seasonStartYear), ApiCache.SizeOf);
        return TeamsVmToTeamsSummaryVmMapper.Map(teamsVm);
    }

    [McpServerTool, Description("Get stats for a specific NHL team by team ID, including game history with model odds and bookmaker lines.")]
    public async Task<TeamVM> GetTeamStats(
        [Description("The team ID")] int teamId,
        [Description("The season start year (e.g. 2024 for the 2024-25 season)")] int seasonStartYear)
        => await cache.GetOrSetAsync(ApiCache.TeamKey(teamId, seasonStartYear),
            () => teamGetter.GetTeamStats(teamId, seasonStartYear), ApiCache.SizeOf, ApiCache.DateRangeLifetime);
}