using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace Search;

/// <summary>A vector the live articles index already holds, and the <see cref="ArticleDocument.EmbeddingKey"/> it was made from.</summary>
public record PreviousEmbedding(string Key, float[] Vector);

/// <summary>
/// Gives articles their vectors for semantic search. The index is rebuilt every hour, but only new or changed
/// articles go to the model: an unchanged one keeps the vector the live index holds (same text and model, same
/// <see cref="ArticleDocument.EmbeddingKey"/>). Newest articles go first, within a time budget, so the first run on
/// a Pi (every article at once) spreads over a few hourly runs instead of holding up the index. A failing embedding
/// server never fails the rebuild: the articles it didn't reach are indexed without vectors.
/// </summary>
public class ArticleEmbeddings(IEmbedder embedder, ILogger logger)
{
    public const int BatchSize = 16;

    /// <summary>
    /// How much of an article goes into its vector: the headline, summary and opening, where NHL.com articles say
    /// what they're about. Longer text costs Pi CPU time and blurs one vector over more topics.
    /// </summary>
    public const int MaxTextChars = 2000;

    public static string EmbeddingText(ArticleDocument article)
    {
        var text = string.Join("\n", new[] { article.Title, article.Summary, article.Body }.Where(s => !string.IsNullOrWhiteSpace(s)));
        return text.Length <= MaxTextChars ? text : text[..MaxTextChars];
    }

    /// <summary>A hash of the model and text: a vector can be reused only while both are the same.</summary>
    public static string EmbeddingKey(string model, string text) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(model + "\n" + text)))[..32];

    /// <summary>The vectors in the live articles index, by entity id. Empty if there's no index or it can't be read.</summary>
    public static async Task<Dictionary<string, PreviousEmbedding>> ReadPrevious(ElasticsearchClient client, ILogger logger,
        CancellationToken ct = default)
    {
        var previous = new Dictionary<string, PreviousEmbedding>();
        // Elasticsearch leaves vectors out of _source unless asked
        var source = new JsonObject
        {
            ["includes"] = new JsonArray("entityId", "embeddingKey", SearchIndexes.ArticleEmbeddingField),
            ["exclude_vectors"] = false,
        };
        try
        {
            await foreach (var hit in client.ReadAll(SearchIndexes.Articles.Alias, source, "entityId", ct))
            {
                var doc = hit["_source"];
                if (doc?["entityId"]?.GetValue<string>() is { } id && doc["embeddingKey"]?.GetValue<string>() is { } key
                    && doc[SearchIndexes.ArticleEmbeddingField] is JsonArray vector && vector.Count > 0)
                    previous[id] = new PreviousEmbedding(key, vector.Select(v => v!.GetValue<float>()).ToArray());
            }
        }
        catch (Exception e) when (e is ElasticsearchException or HttpRequestException
            || e is TaskCanceledException && !ct.IsCancellationRequested)
        {
            // Every article goes to the model again; slow, but the index still gets built
            logger.LogWarning(e, "Couldn't read the live article vectors; embedding every article again");
            return [];
        }
        return previous;
    }

    /// <summary>
    /// The articles with <see cref="ArticleDocument.EmbeddingKey"/> set and, where reused or made in time, a vector.
    /// All vectors in the result have the same length.
    /// </summary>
    public async Task<List<ArticleDocument>> Add(IReadOnlyList<ArticleDocument> articles,
        IReadOnlyDictionary<string, PreviousEmbedding> previous, TimeSpan budget, CancellationToken ct = default)
    {
        var result = new ArticleDocument[articles.Count];
        var pending = new List<(int Index, string Text, DateTime? Date)>();
        for (var i = 0; i < articles.Count; i++)
        {
            var article = articles[i];
            var text = EmbeddingText(article);
            var key = EmbeddingKey(embedder.Model, text);
            if (previous.TryGetValue(article.EntityId, out var old) && old.Key == key)
                result[i] = article with { EmbeddingKey = key, Embedding = old.Vector };
            else
            {
                result[i] = article with { EmbeddingKey = key, Embedding = null };
                pending.Add((i, text, article.Date));
            }
        }
        var reused = articles.Count - pending.Count;

        var embedded = 0;
        var watch = Stopwatch.StartNew();
        foreach (var batch in pending.OrderByDescending(p => p.Date).Chunk(BatchSize))
        {
            if (watch.Elapsed > budget)
            {
                logger.LogInformation("Embedding time budget ({Budget}) used up; the next run continues", budget);
                break;
            }
            float[][] vectors;
            try
            {
                vectors = await embedder.Embed(batch.Select(p => p.Text).ToList(), EmbeddingPurpose.Document, ct);
            }
            catch (Exception e) when (e is EmbeddingException or HttpRequestException
                || e is TaskCanceledException && !ct.IsCancellationRequested)
            {
                logger.LogWarning(e, "Embedding articles with {Model} failed; indexing the rest without vectors", embedder.Model);
                break;
            }
            for (var j = 0; j < batch.Length; j++)
                result[batch[j].Index] = result[batch[j].Index] with { Embedding = vectors[j] };
            embedded += batch.Length;
        }

        // One dense_vector field has one size. A reused vector from another size can't happen (the key includes the
        // model), but a model swapped under the same name could: the newest vectors win
        var dims = result.Where(a => a.Embedding != null).OrderByDescending(a => a.Date).Select(a => a.Embedding!.Length).FirstOrDefault();
        var dropped = 0;
        for (var i = 0; i < result.Length; i++)
            if (result[i].Embedding is { } v && v.Length != dims)
            {
                result[i] = result[i] with { Embedding = null };
                dropped++;
            }

        logger.LogInformation(
            "Article vectors ({Model}): {Reused} reused, {Embedded} embedded in {Seconds:F0}s, {Missing} without, {Dropped} dropped for size",
            embedder.Model, reused, embedded, watch.Elapsed.TotalSeconds, result.Count(a => a.Embedding == null), dropped);
        return result.ToList();
    }
}
