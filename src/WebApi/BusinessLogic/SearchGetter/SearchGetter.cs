using Microsoft.Extensions.Caching.Memory;
using Search;

namespace WebApi.BusinessLogic.SearchGetter;

public interface ISearchGetter
{
    Task<SearchResults> Search(string query, CancellationToken ct);

    /// <summary>A few teams, players and games for the search box as you type (no articles, no embedding).</summary>
    Task<SuggestResults> Suggest(string query, CancellationToken ct);
}

/// <summary>
/// Site search over Elasticsearch (the indices RUN_MODE=SearchIndex builds), articles by keyword and, with an
/// embedding server, by meaning too. Results are cached briefly in their own
/// size-limited cache, so a flood of distinct queries can't push the API's season data out of <see cref="Caching.ApiCache"/>.
/// </summary>
public class SearchGetter(ElasticsearchClient client, [FromKeyedServices(SearchGetter.CacheKey)] IMemoryCache cache, TimeProvider time,
    IQueryEmbedder embedder) : ISearchGetter
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

        var (nowUtc, parsed) = Parse(normalized);
        // The words without the date, for the semantic half of article search (null: keyword only)
        var vector = parsed.Text.Length >= SiteSearch.MinQueryChars ? await embedder.TryEmbed(parsed.Text, ct) : null;
        var response = await client.MultiSearch(SiteSearch.BuildRequest(parsed, nowUtc, vector, embedder.MinSimilarity), ct);
        var results = SiteSearch.ReadResponse(query.Trim(), response);

        cache.Set(normalized, results, new MemoryCacheEntryOptions { Size = 1, AbsoluteExpirationRelativeToNow = CacheLifetime });
        return results;
    }

    public async Task<SuggestResults> Suggest(string query, CancellationToken ct)
    {
        var normalized = query.Trim().ToLowerInvariant();
        // Its own keys in the same cache: a suggestion and a full search for the same text are different answers
        var key = "suggest:" + normalized;
        if (cache.TryGetValue(key, out SuggestResults? cached) && cached != null)
            return cached with { Query = query.Trim() };

        var (nowUtc, parsed) = Parse(normalized);
        var response = await client.MultiSearch(SiteSearch.BuildSuggestRequest(parsed, nowUtc), ct);
        var results = SiteSearch.ReadSuggestions(query.Trim(), response);

        cache.Set(key, results, new MemoryCacheEntryOptions { Size = 1, AbsoluteExpirationRelativeToNow = CacheLifetime });
        return results;
    }

    private (DateTime NowUtc, ParsedQuery Parsed) Parse(string normalized)
    {
        var nowUtc = time.GetUtcNow().UtcDateTime;
        // Dates in a query ("tonight", "march 3") are the NHL's schedule dates, in Eastern time
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(nowUtc, Eastern));
        return (nowUtc, SiteSearch.Parse(normalized, today));
    }
}
