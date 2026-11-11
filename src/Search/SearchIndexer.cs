using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Search;

/// <summary>
/// Rebuilds an index from scratch without downtime: documents go into a new timestamped index, then the alias the
/// site searches moves to it in one atomic step and the old index is deleted. A failed build leaves the old index
/// (and so search) as it was. A build whose documents and mapping match the live index's is skipped.
/// </summary>
public class SearchIndexer(ElasticsearchClient client, ILogger<SearchIndexer> logger)
{
    private const int BatchSize = 1000;

    // How long to wait for the new index's replica before moving the alias anyway (searches then run on the
    // primary alone until the replica catches up)
    private static readonly TimeSpan ReplicaWait = TimeSpan.FromMinutes(2);

    /// <returns>False if the live index already had exactly these documents and mapping, so nothing was rebuilt</returns>
    public async Task<bool> Rebuild<T>(SearchIndex index, IReadOnlyCollection<T> documents, Func<T, string> id, DateTime nowUtc,
        CancellationToken ct = default) where T : notnull
    {
        // Most hours nothing changed: skip the rebuild rather than rewrite the same documents on the search nodes
        var hash = ContentHash(index, documents, id);
        if (await client.GetMappingMeta(index.Alias, SearchIndex.ContentHashKey, ct) == hash)
        {
            logger.LogInformation("{Alias} unchanged ({Count} documents), not rebuilt", index.Alias, documents.Count);
            return false;
        }

        var name = index.NewIndexName(nowUtc);
        await client.CreateIndex(name, index.CreateBody(hash), ct);
        try
        {
            foreach (var batch in documents.Chunk(BatchSize))
                await client.Bulk(name, batch.Select(d => (id(d), (object)d)), ct);
            await client.Refresh(name, ct);

            // The replica copies the finished segments instead of indexing every document a second time
            await client.UpdateSettings(name, SearchIndex.LiveSettings(), ct);
            if (!await client.WaitForShardCopies(name, ReplicaWait, ct))
                logger.LogWarning("{Index}'s replica wasn't ready after {Wait}; moving the alias anyway", name, ReplicaWait);

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
        return true;
    }

    /// <summary>
    /// A hash of the create-index body and every document (as indexed), independent of document order, so the same
    /// rows read in a different order hash the same.
    /// </summary>
    public static string ContentHash<T>(SearchIndex index, IEnumerable<T> documents, Func<T, string> id) where T : notnull
    {
        var lines = documents
            .Select(d => id(d) + "\n" + JsonSerializer.Serialize(d, d.GetType(), SearchJson.Options))
            .Order(StringComparer.Ordinal);
        using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        sha.AppendData(Encoding.UTF8.GetBytes(index.CreateBody().ToJsonString()));
        foreach (var line in lines)
        {
            sha.AppendData([0]);
            sha.AppendData(Encoding.UTF8.GetBytes(line));
        }
        return Convert.ToHexStringLower(sha.GetHashAndReset());
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
