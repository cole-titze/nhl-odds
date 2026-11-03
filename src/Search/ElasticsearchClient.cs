using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Search;

/// <summary>Where Elasticsearch is and who to log in as (ELASTICSEARCH_URL, ELASTICSEARCH_USERNAME, ELASTICSEARCH_PASSWORD).</summary>
public record ElasticsearchSettings(string Url, string? Username, string? Password)
{
    public const string UrlKey = "ELASTICSEARCH_URL";
    public const string UsernameKey = "ELASTICSEARCH_USERNAME";
    public const string PasswordKey = "ELASTICSEARCH_PASSWORD";

    /// <summary>Null when no URL is set, which turns search off.</summary>
    public static ElasticsearchSettings? From(Func<string, string?> get)
    {
        var url = get(UrlKey);
        return string.IsNullOrWhiteSpace(url) ? null : new ElasticsearchSettings(url.TrimEnd('/'), get(UsernameKey), get(PasswordKey));
    }
}

public class ElasticsearchException(string message) : Exception(message);

/// <summary>
/// The handful of Elasticsearch REST calls the site uses, over plain HTTP + JSON. The official client would hide
/// the request bodies; here every query is the JSON Elasticsearch documents, which is easier to learn from.
/// </summary>
public class ElasticsearchClient
{
    private readonly HttpClient _http;

    /// <param name="http">Its BaseAddress is the cluster URL</param>
    public ElasticsearchClient(HttpClient http, ElasticsearchSettings settings)
    {
        _http = http;
        _http.BaseAddress ??= new Uri(settings.Url + "/");
        if (!string.IsNullOrEmpty(settings.Username))
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes($"{settings.Username}:{settings.Password}")));
    }

    public async Task CreateIndex(string name, JsonObject body, CancellationToken ct = default)
    {
        using var response = await _http.PutAsync(name, Json(body), ct);
        await EnsureSuccess(response, $"create index {name}", ct);
    }

    public async Task DeleteIndex(string name, CancellationToken ct = default)
    {
        using var response = await _http.DeleteAsync(name, ct);
        if (response.StatusCode != HttpStatusCode.NotFound)
            await EnsureSuccess(response, $"delete index {name}", ct);
    }

    /// <summary>Indexes documents with one _bulk request. Throws if any document failed.</summary>
    public async Task Bulk(string index, IEnumerable<(string Id, object Document)> documents, CancellationToken ct = default)
    {
        var ndjson = new StringBuilder();
        foreach (var (id, document) in documents)
        {
            ndjson.Append(JsonSerializer.Serialize(new { index = new { _index = index, _id = id } })).Append('\n');
            ndjson.Append(JsonSerializer.Serialize(document, document.GetType(), SearchJson.Options)).Append('\n');
        }
        if (ndjson.Length == 0)
            return;

        using var response = await _http.PostAsync("_bulk", new StringContent(ndjson.ToString(), Encoding.UTF8, "application/x-ndjson"), ct);
        await EnsureSuccess(response, "bulk", ct);
        // A 200 can still carry per-document failures (e.g. a mapping conflict)
        var result = JsonNode.Parse(await response.Content.ReadAsStringAsync(ct))!;
        if (result["errors"]?.GetValue<bool>() == true)
        {
            var firstError = result["items"]!.AsArray()
                .Select(item => item!["index"]?["error"])
                .FirstOrDefault(error => error != null);
            throw new ElasticsearchException($"bulk into {index} had failures, first: {firstError?.ToJsonString()}");
        }
    }

    /// <summary>Makes indexed documents searchable now instead of within the refresh interval.</summary>
    public async Task Refresh(string index, CancellationToken ct = default)
    {
        using var response = await _http.PostAsync($"{index}/_refresh", null, ct);
        await EnsureSuccess(response, $"refresh {index}", ct);
    }

    /// <summary>The indices an alias points at (none if the alias doesn't exist yet).</summary>
    public async Task<List<string>> GetAliasIndices(string alias, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync($"_alias/{alias}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return [];
        await EnsureSuccess(response, $"get alias {alias}", ct);
        var result = JsonNode.Parse(await response.Content.ReadAsStringAsync(ct))!.AsObject();
        return result.Select(kv => kv.Key).ToList();
    }

    /// <summary>
    /// Points <paramref name="alias"/> at <paramref name="index"/> alone, in one atomic _aliases call, so searches
    /// see either the old index or the new one, never neither.
    /// </summary>
    public async Task SwapAlias(string alias, string index, IEnumerable<string> oldIndices, CancellationToken ct = default)
    {
        var actions = new JsonArray();
        foreach (var old in oldIndices)
            actions.Add(new JsonObject { ["remove"] = new JsonObject { ["index"] = old, ["alias"] = alias } });
        actions.Add(new JsonObject { ["add"] = new JsonObject { ["index"] = index, ["alias"] = alias } });

        using var response = await _http.PostAsync("_aliases", Json(new JsonObject { ["actions"] = actions }), ct);
        await EnsureSuccess(response, $"swap alias {alias}", ct);
    }

    /// <summary>
    /// The names of indices matching a pattern such as <c>nhl-games-*</c>. Uses the get-index API (an index
    /// privilege) rather than _cat/indices, which needs the cluster-wide monitor privilege.
    /// </summary>
    public async Task<List<string>> GetIndices(string pattern, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync($"{pattern}?filter_path=*.settings.index.provided_name", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return [];
        await EnsureSuccess(response, $"list indices {pattern}", ct);
        var result = JsonNode.Parse(await response.Content.ReadAsStringAsync(ct)) as JsonObject;
        return result?.Select(kv => kv.Key).ToList() ?? [];
    }

    /// <summary>Runs several searches in one round trip (_msearch). Body is header/query line pairs.</summary>
    public async Task<JsonNode> MultiSearch(string ndjson, CancellationToken ct = default)
    {
        using var response = await _http.PostAsync("_msearch", new StringContent(ndjson, Encoding.UTF8, "application/x-ndjson"), ct);
        await EnsureSuccess(response, "msearch", ct);
        return JsonNode.Parse(await response.Content.ReadAsStringAsync(ct))!;
    }

    private static StringContent Json(JsonNode body) =>
        new(body.ToJsonString(), Encoding.UTF8, "application/json");

    private static async Task EnsureSuccess(HttpResponseMessage response, string what, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
            return;
        var body = await response.Content.ReadAsStringAsync(ct);
        throw new ElasticsearchException($"Elasticsearch {what} failed: {(int)response.StatusCode} {body[..Math.Min(body.Length, 1000)]}");
    }
}

public static class SearchJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}
