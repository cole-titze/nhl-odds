using Microsoft.Extensions.Caching.Memory;
using Search;

namespace WebApi.BusinessLogic.SearchGetter;

public interface ISearchGetter
{
    Task<SearchResults> Search(string query, CancellationToken ct);
}

/// <summary>
/// Site search over Elasticsearch (the indices RUN_MODE=SearchIndex builds). Results are cached briefly in their own
/// size-limited cache, so a flood of distinct queries can't push the API's season data out of <see cref="Caching.ApiCache"/>.
/// </summary>
public class SearchGetter(ElasticsearchClient client, [FromKeyedServices(SearchGetter.CacheKey)] IMemoryCache cache, TimeProvider time)
    : ISearchGetter
{
    public const string CacheKey = "search";
    public const long CacheEntries = 500;
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(2);
    private static readonly TimeZoneInfo Eastern = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    public static IMemoryCache CreateCache() => new MemoryCache(new MemoryCacheOptions { SizeLimit = CacheEntries });

    public async Task<SearchResults> Search(string query, CancellationToken ct)
    {
        var normalized = query.Trim().ToLowerInvariant();
        if (cache.TryGetValue(normalized, out SearchResults? cached) && cached != null)
            return cached with { Query = query.Trim() };

        var nowUtc = time.GetUtcNow().UtcDateTime;
        // Dates in a query ("tonight", "march 3") are the NHL's schedule dates, in Eastern time
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(nowUtc, Eastern));
        var parsed = SiteSearch.Parse(normalized, today);
        var response = await client.MultiSearch(SiteSearch.BuildRequest(parsed, nowUtc), ct);
        var results = SiteSearch.ReadResponse(query.Trim(), response);

        cache.Set(normalized, results, new MemoryCacheEntryOptions { Size = 1, AbsoluteExpirationRelativeToNow = CacheLifetime });
        return results;
    }
}
