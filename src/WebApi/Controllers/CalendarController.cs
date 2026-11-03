using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using WebApi.BusinessLogic.TeamGetter;
using WebApi.Caching;
using WebApi.Calendar;

namespace WebApi.Controllers;

// Calendar feeds for calendar apps to subscribe to (the Team page links them with webcal://)
[Route("api/[controller]")]
[ApiController]
public class CalendarController(
    ITeamGetter teamGetter,
    [FromKeyedServices(ApiCache.ServiceKey)] IMemoryCache cache,
    IConfiguration config)
{
    // The public site the events link to; SITE_URL overrides it (say, for a staging copy)
    public const string DefaultSiteUrl = "https://nhl.half-built.com";

    // A team's games this season, built from the cached GetAllTeams response the StartupCacheWarmer keeps
    [HttpGet("Team/{teamId:int}.ics")]
    public async Task<IResult> Team(int teamId)
    {
        var now = DateTime.UtcNow;
        var season = Seasons.Current(now);
        var teamsVm = await cache.GetOrSetAsync(ApiCache.AllTeamsKey(season),
            () => teamGetter.GetAllTeamsStats(season), ApiCache.SizeOf);
        var team = teamsVm.Teams.FirstOrDefault(t => t.Id == teamId);
        if (team is null)
            return Results.NotFound();

        var siteUrl = config["SITE_URL"] is { Length: > 0 } configured ? configured : DefaultSiteUrl;
        var ics = TeamCalendarBuilder.Build(team, season, siteUrl, now);
        return new CalendarResult(ics, $"{team.TeamName.ToLowerInvariant().Replace(' ', '-')}.ics");
    }

    private sealed class CalendarResult(string ics, string fileName) : IResult
    {
        public Task ExecuteAsync(HttpContext httpContext)
        {
            var headers = httpContext.Response.Headers;
            headers.CacheControl = "public, max-age=3600";
            headers.ContentDisposition = $"inline; filename=\"{fileName}\"";
            return Results.Text(ics, "text/calendar; charset=utf-8").ExecuteAsync(httpContext);
        }
    }
}
