using Microsoft.AspNetCore.Mvc;
using Prometheus;
using Search;
using WebApi.BusinessLogic.SearchGetter;

namespace WebApi.Controllers;

/// <summary>
/// Site search: teams, players, games and NHL.com articles in one call. Off (404) unless ELASTICSEARCH_URL is set.
/// Uses the "api" rate limit like the other endpoints.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class SearchController(IServiceProvider services, ILogger<SearchController> logger)
{
    private static readonly Counter Requests = Metrics.CreateCounter(
        "search_requests_total", "Site search requests", "result");

    [HttpGet]
    public async Task<IResult> Get(string? q, CancellationToken cancellationToken)
    {
        var search = services.GetService<ISearchGetter>();
        if (search == null)
            return Results.NotFound(new { message = "Search is turned off." });

        var query = q?.Trim() ?? "";
        if (query.Length is < SiteSearch.MinQueryChars or > SiteSearch.MaxQueryChars)
        {
            Requests.WithLabels("invalid").Inc();
            return Results.BadRequest(new { message = $"Search for {SiteSearch.MinQueryChars} to {SiteSearch.MaxQueryChars} characters." });
        }

        try
        {
            var results = await search.Search(query, cancellationToken);
            Requests.WithLabels("ok").Inc();
            return Results.Ok(results);
        }
        catch (Exception e) when (e is ElasticsearchException or HttpRequestException
            || e is TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(e, "Search failed for {Query}", query);
            Requests.WithLabels("error").Inc();
            return Results.Json(new { message = "Search isn't available right now." }, statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }
}
