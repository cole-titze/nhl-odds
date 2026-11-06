using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace Search;

/// <summary>
/// The Ollama server that turns text into vectors for semantic article search (EMBEDDING_URL, EMBEDDING_MODEL,
/// EMBEDDING_MIN_SIMILARITY). Separate from the assistant's OLLAMA_URL: embeddings run on a small model the cluster
/// can serve itself, so search doesn't depend on the Mac being awake.
/// </summary>
/// <param name="MinSimilarity">The least cosine similarity a vector match needs to count. Below it, knn's nearest
/// neighbours are just the least unrelated articles, which would show for every query</param>
public record EmbeddingSettings(string Url, string Model, double MinSimilarity)
{
    public const string UrlKey = "EMBEDDING_URL";
    public const string ModelKey = "EMBEDDING_MODEL";
    public const string MinSimilarityKey = "EMBEDDING_MIN_SIMILARITY";
    public const string DefaultModel = "nomic-embed-text";
    public const double DefaultMinSimilarity = 0.55;

    /// <summary>Null when no URL is set, which turns vector search off (keyword search still works).</summary>
    public static EmbeddingSettings? From(Func<string, string?> get)
    {
        var url = get(UrlKey);
        if (string.IsNullOrWhiteSpace(url))
            return null;
        var model = get(ModelKey) is { Length: > 0 } m ? m : DefaultModel;
        var minSimilarity = double.TryParse(get(MinSimilarityKey), NumberStyles.Float, CultureInfo.InvariantCulture, out var s)
            ? s
            : DefaultMinSimilarity;
        return new EmbeddingSettings(url.TrimEnd('/'), model, minSimilarity);
    }
}

public enum EmbeddingPurpose { Query, Document }

/// <summary>
/// Retrieval models are trained with a prefix telling a query from a document; without it matches get noticeably
/// worse. Keyed by the model name without its tag ("nomic-embed-text:v1.5" → "nomic-embed-text").
/// </summary>
public static class EmbeddingPrompts
{
    private static readonly Dictionary<string, (string Query, string Document)> Prefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["nomic-embed-text"] = ("search_query: ", "search_document: "),
        ["embeddinggemma"] = ("task: search result | query: ", "title: none | text: "),
        ["qwen3-embedding"] = ("Instruct: Given a hockey search query, retrieve news articles that answer it\nQuery: ", ""),
    };

    public static string Prefix(string model, EmbeddingPurpose purpose)
    {
        var name = model.Split(':')[0];
        if (name.Contains('/'))
            name = name[(name.LastIndexOf('/') + 1)..];
        return Prefixes.TryGetValue(name, out var p) ? (purpose == EmbeddingPurpose.Query ? p.Query : p.Document) : "";
    }
}

public class EmbeddingException(string message) : Exception(message);

public interface IEmbedder
{
    string Model { get; }
    /// <summary>One vector per text, in order. Throws <see cref="EmbeddingException"/> or <see cref="HttpRequestException"/> on failure.</summary>
    Task<float[][]> Embed(IReadOnlyList<string> texts, EmbeddingPurpose purpose, CancellationToken ct = default);
}

/// <summary>Ollama's /api/embed: a batch of texts in, one normalized vector per text out.</summary>
public class OllamaEmbedder : IEmbedder
{
    private readonly HttpClient _http;

    public OllamaEmbedder(HttpClient http, EmbeddingSettings settings)
    {
        _http = http;
        _http.BaseAddress ??= new Uri(settings.Url + "/");
        Model = settings.Model;
    }

    public string Model { get; }

    public async Task<float[][]> Embed(IReadOnlyList<string> texts, EmbeddingPurpose purpose, CancellationToken ct = default)
    {
        if (texts.Count == 0)
            return [];
        var prefix = EmbeddingPrompts.Prefix(Model, purpose);
        var body = new JsonObject
        {
            ["model"] = Model,
            ["input"] = new JsonArray(texts.Select(t => (JsonNode)(prefix + t)).ToArray()),
            // Cut a text longer than the model's context instead of failing the batch
            ["truncate"] = true,
        };
        // A sized body rather than PostAsJsonAsync's chunked stream, which some proxies and servers refuse
        using var response = await _http.PostAsync("api/embed",
            new StringContent(body.ToJsonString(), System.Text.Encoding.UTF8, "application/json"), ct);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            throw new EmbeddingException($"Ollama embed with {Model} failed: {(int)response.StatusCode} {error[..Math.Min(error.Length, 500)]}");
        }

        var result = await response.Content.ReadFromJsonAsync<EmbedResponse>(SearchJson.Options, ct);
        if (result?.Embeddings is not { } embeddings || embeddings.Length != texts.Count || embeddings.Any(e => e.Length == 0))
            throw new EmbeddingException($"Ollama returned {result?.Embeddings?.Length ?? 0} embeddings for {texts.Count} texts");
        return embeddings;
    }

    private record EmbedResponse(float[][]? Embeddings);
}
