using Prometheus;
using Search;

namespace WebApi.BusinessLogic.SearchGetter;

/// <summary>Turns a search into a vector for the semantic half of article search, or null to search by keyword alone.</summary>
public interface IQueryEmbedder
{
    double MinSimilarity { get; }
    Task<float[]?> TryEmbed(string text, CancellationToken ct);
}

/// <summary>Vector search off (no EMBEDDING_URL): every search is keyword-only.</summary>
public class NoQueryEmbedder : IQueryEmbedder
{
    public double MinSimilarity => EmbeddingSettings.DefaultMinSimilarity;
    public Task<float[]?> TryEmbed(string text, CancellationToken ct) => Task.FromResult<float[]?>(null);
}

/// <summary>
/// When the embedding server last failed. Singleton, so one failure makes every search skip the server for
/// <see cref="Pause"/> instead of each waiting out its own timeout while the model is down.
/// </summary>
public class EmbeddingCircuit(TimeProvider time)
{
    public static readonly TimeSpan Pause = TimeSpan.FromMinutes(1);
    private long _openUntilTicks;

    public bool IsOpen => time.GetUtcNow().UtcTicks < Interlocked.Read(ref _openUntilTicks);
    public void Trip() => Interlocked.Exchange(ref _openUntilTicks, (time.GetUtcNow() + Pause).UtcTicks);
}

/// <summary>
/// Embeds the query with the model the articles were embedded with. Search must stay fast, so it gives up after
/// <see cref="Timeout"/> and the search goes on by keyword; a failure pauses embedding (<see cref="EmbeddingCircuit"/>).
/// </summary>
public class QueryEmbedder(IEmbedder embedder, EmbeddingSettings settings, EmbeddingCircuit circuit, ILogger<QueryEmbedder> logger)
    : IQueryEmbedder
{
    // A short query on a loaded model takes well under 100 ms on a Pi; past this the model is loading or stuck
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);

    private static readonly Counter Embeddings = Metrics.CreateCounter(
        "search_embedding_total", "Search queries sent to the embedding model", "result");

    public double MinSimilarity => settings.MinSimilarity;

    public async Task<float[]?> TryEmbed(string text, CancellationToken ct)
    {
        if (circuit.IsOpen)
        {
            Embeddings.WithLabels("skipped").Inc();
            return null;
        }
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(Timeout);
        try
        {
            var vectors = await embedder.Embed([text], EmbeddingPurpose.Query, timeout.Token);
            Embeddings.WithLabels("ok").Inc();
            return vectors[0];
        }
        catch (Exception e) when (e is EmbeddingException or HttpRequestException
            || e is OperationCanceledException && !ct.IsCancellationRequested)
        {
            logger.LogWarning(e, "Embedding the search failed; keyword search only for {Pause}", EmbeddingCircuit.Pause);
            circuit.Trip();
            Embeddings.WithLabels("error").Inc();
            return null;
        }
    }
}
