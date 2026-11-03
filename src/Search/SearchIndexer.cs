using Microsoft.Extensions.Logging;

namespace Search;

/// <summary>
/// Rebuilds an index from scratch without downtime: documents go into a new timestamped index, then the alias the
/// site searches moves to it in one atomic step and the old index is deleted. A failed build leaves the old index
/// (and so search) as it was.
/// </summary>
public class SearchIndexer(ElasticsearchClient client, ILogger<SearchIndexer> logger)
{
    private const int BatchSize = 1000;

    public async Task Rebuild<T>(SearchIndex index, IReadOnlyCollection<T> documents, Func<T, string> id, DateTime nowUtc,
        CancellationToken ct = default) where T : notnull
    {
        var name = index.NewIndexName(nowUtc);
        await client.CreateIndex(name, index.CreateBody(), ct);
        try
        {
            foreach (var batch in documents.Chunk(BatchSize))
                await client.Bulk(name, batch.Select(d => (id(d), (object)d)), ct);
            await client.Refresh(name, ct);

            var old = await client.GetAliasIndices(index.Alias, ct);
            await client.SwapAlias(index.Alias, name, old, ct);
        }
        catch
        {
            await TryDelete(name);
            throw;
        }
        logger.LogInformation("Indexed {Count} documents into {Index} ({Alias})", documents.Count, name, index.Alias);

        // After the swap, so a failure here can't touch the live index; the next run retries it
        try
        {
            // Also catches an index a crashed run created but never pointed the alias at
            foreach (var leftover in (await client.GetIndices(index.Alias + "-*", ct)).Where(i => i != name))
                await client.DeleteIndex(leftover, ct);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            logger.LogWarning(e, "Couldn't delete the old {Alias} indices", index.Alias);
        }
    }

    private async Task TryDelete(string name)
    {
        try
        {
            await client.DeleteIndex(name);
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Couldn't delete the unfinished index {Index}", name);
        }
    }
}
