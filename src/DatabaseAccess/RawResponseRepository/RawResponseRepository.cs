using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Entities.Types;
using Microsoft.EntityFrameworkCore;

namespace DatabaseAccess.RawResponseRepository;

/// <summary>
/// Keeps raw NHL API responses gzipped in NhlApiResponse. Each call uses its own DbContext, since games and replays
/// are fetched side by side.
/// </summary>
public class RawResponseRepository : IRawResponseStore
{
    // A game's copy only counts once the game is surely over: fetched at least this long after its start
    public static readonly TimeSpan GAME_OVER_AFTER = TimeSpan.FromHours(8);
    private readonly Func<NhlDbContext> _dbContextFactory;

    public RawResponseRepository(Func<NhlDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<string?> GetLatest(string url, int? gameId)
    {
        await using var db = _dbContextFactory();
        var responses = db.NhlApiResponse.AsNoTracking().Where(r => r.Url == url);
        if (gameId != null)
        {
            responses = responses.Where(r => db.GameRaw.Any(g => g.Id == gameId && g.HasBeenPlayed
                && r.LastSeenUTC >= g.GameDateUTC + GAME_OVER_AFTER));
        }
        var body = await responses.OrderByDescending(r => r.LastSeenUTC).Select(r => r.Body).FirstOrDefaultAsync();
        return body == null ? null : Decompress(body);
    }

    public async Task Save(string url, int? gameId, string body, DateTime fetchedUtc)
    {
        await using var db = _dbContextFactory();
        var hash = Hash(body);
        var gzipped = Compress(body);
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "NhlApiResponse" ("Url", "ContentHash", "GameId", "FirstSeenUTC", "LastSeenUTC", "Body")
            VALUES ({url}, {hash}, {gameId}, {fetchedUtc}, {fetchedUtc}, {gzipped})
            ON CONFLICT ("Url", "ContentHash") DO UPDATE SET "LastSeenUTC" = EXCLUDED."LastSeenUTC"
            """);
    }

    public static string Hash(string body) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(body)));

    public static byte[] Compress(string body)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.SmallestSize))
            gzip.Write(Encoding.UTF8.GetBytes(body));
        return output.ToArray();
    }

    public static string Decompress(byte[] gzipped)
    {
        using var gzip = new GZipStream(new MemoryStream(gzipped), CompressionMode.Decompress);
        using var reader = new StreamReader(gzip, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}