using System.ComponentModel;
using Entities.ViewModels;
using ModelContextProtocol.Server;
using WebApi.BusinessLogic.AdminService;
using WebApi.BusinessLogic.TeamGetter;

namespace WebApi.McpTools;

[McpServerToolType]
public class TeamMcpTools(ITeamGetter teamGetter, IAdminService adminService)
{
    [McpServerTool, Description("Get all NHL teams and their season stats, win/loss records, and model accuracy.")]
    public async Task<TeamsVM> GetAllTeams(
        [Description("The season start year (e.g. 2024 for the 2024-25 season)")] int seasonStartYear)
        => await teamGetter.GetAllTeamsStats(seasonStartYear);

    [McpServerTool, Description("Get stats for a specific NHL team by team ID, including game history with model odds and bookmaker lines.")]
    public async Task<TeamVM> GetTeamStats(
        [Description("The team ID")] int teamId,
        [Description("The season start year (e.g. 2024 for the 2024-25 season)")] int seasonStartYear)
        => await teamGetter.GetTeamStats(teamId, seasonStartYear);

    [McpServerTool, Description("Get data health checks showing missing predictions, odds, and errors per season.")]
    public async Task<List<SeasonHealthCheckVM>> GetHealthChecks()
        => await adminService.GetHealthChecks();
}