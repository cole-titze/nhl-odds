using Microsoft.AspNetCore.Mvc;
using Prometheus;
using Search;
using WebApi.BusinessLogic.SearchGetter;

namespace WebApi.Controllers;

/// <summary>
/// Site search: teams, players, games and NHL.com articles in one call, and lighter suggestions for the search box
/// as you type. Off (404) unless ELASTICSEARCH_URL is set. Uses the "api" rate limit like the other endpoints.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class SearchController(IServiceProvider services, ILogger<SearchController> logger)
{
    private static readonly Counter Requests = Metrics.CreateCounter(
        "search_requests_total", "Site search requests", "result");

    private static readonly Counter SuggestRequests = Metrics.CreateCounter(
        "search_suggest_requests_total", "Search box suggestion requests", "result");

    [HttpGet]
    public Task<IResult> Get(string? q, CancellationToken cancellationToken) =>
        Run(q, Requests, (search, query) => search.Search(query, cancellationToken), cancellationToken);

    /// <summary>A few teams, players and games matching what's been typed so far (no articles).</summary>
    [HttpGet("Suggest")]
    public Task<IResult> Suggest(string? q, CancellationToken cancellationToken) =>
        Run(q, SuggestRequests, (search, query) => search.Suggest(query, cancellationToken), cancellationToken);

    private async Task<IResult> Run<T>(string? q, Counter requests, Func<ISearchGetter, string, Task<T>> run,
        CancellationToken cancellationToken)
    {
        var search = services.GetService<ISearchGetter>();
        if (search == null)
            return Results.NotFound(new { message = "Search is turned off." });

        var query = q?.Trim() ?? "";
        if (query.Length is < SiteSearch.MinQueryChars or > SiteSearch.MaxQueryChars)
        {
            requests.WithLabels("invalid").Inc();
            return Results.BadRequest(new { message = $"Search for {SiteSearch.MinQueryChars} to {SiteSearch.MaxQueryChars} characters." });
        }

        try
        {
            var results = await run(search, query);
            requests.WithLabels("ok").Inc();
            return Results.Ok(results);
        }
        catch (Exception e) when (e is ElasticsearchException or HttpRequestException
            || e is TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(e, "Search failed for {Query}", query);
            requests.WithLabels("error").Inc();
            return Results.Json(new { message = "Search isn't available right now." }, statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }
}
