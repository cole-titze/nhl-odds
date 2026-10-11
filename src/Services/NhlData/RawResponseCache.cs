using System.Net;
using System.Text.RegularExpressions;
using Entities.Types;
using Entities.Types.Enums;
using Microsoft.Extensions.Logging;

namespace Services.NhlData;

/// <summary>
/// Stores the raw text of the NHL API endpoints worth keeping (per-game endpoints, shift charts, goal replays, player
/// pages) and, depending on the run's policy, answers from the stored copy instead of the API. The store is a
/// convenience: if it can't be read or written, requests go on as if it weren't there.
/// </summary>
public partial class RawResponseCache
{
    private static readonly string[] STORED_PREFIXES =
    [
        "https://api-web.nhle.com/v1/gamecenter/",
        "https://api-web.nhle.com/v1/player/",
        "https://api.nhle.com/stats/rest/en/shiftcharts",
        "https://wsr.nhle.com/sprites/",
    ];
    private readonly IRawResponseStore _store;
    private readonly ILogger<RawResponseCache> _logger;
    private int _storedHits, _apiCalls, _missing, _storeErrors;

    public RawResponseCache(IRawResponseStore store, RawResponsePolicy policy, ILoggerFactory loggerFactory)
    {
        _store = store;
        Policy = policy;
        _logger = loggerFactory.CreateLogger<RawResponseCache>();
    }

    public RawResponsePolicy Policy { get; }

    public static bool IsStored(string url) => STORED_PREFIXES.Any(url.StartsWith);

    /// <summary>The game id in a game's URL (gamecenter/2023020204/..., gameId=2023020204, sprites/.../2023020204/...).</summary>
    public static int? GameIdOf(string url)
    {
        var match = GameIdRegex().Match(url);
        return match.Success ? int.Parse(match.Value) : null;
    }

    /// <summary>
    /// The response for this URL: from the store when the policy allows and there's a usable copy, else from
    /// <paramref name="fetch"/> (stored when it's a 200). Under StoredOnly a stored endpoint without a copy throws
    /// <see cref="StoredResponseMissingException"/> instead of asking the API.
    /// </summary>
    public async Task<(HttpStatusCode Status, string? Body)> Get(string url, Func<Task<(HttpStatusCode Status, string? Body)>> fetch)
    {
        if (!IsStored(url))
            return await fetch();

        var gameId = GameIdOf(url);
        if (Policy != RawResponsePolicy.Fresh)
        {
            string? stored = null;
            try
            {
                stored = await _store.GetLatest(url, gameId);
            }
            catch (Exception ex)
            {
                StoreError(ex, "read", url);
            }
            if (stored != null)
            {
                Interlocked.Increment(ref _storedHits);
                return (HttpStatusCode.OK, stored);
            }
            if (Policy == RawResponsePolicy.StoredOnly)
            {
                Interlocked.Increment(ref _missing);
                throw new StoredResponseMissingException(url);
            }
        }

        var fetchedUtc = DateTime.UtcNow;
        var (status, body) = await fetch();
        Interlocked.Increment(ref _apiCalls);
        if (status == HttpStatusCode.OK && body != null)
        {
            try
            {
                await _store.Save(url, gameId, body, fetchedUtc);
            }
            catch (Exception ex)
            {
                StoreError(ex, "save", url);
            }
        }
        return (status, body);
    }

    public void LogTotals()
    {
        _logger.LogInformation("Raw responses ({Policy}): {Stored} from the store, {Api} from the API, {Missing} not stored, {Errors} store errors",
            Policy, _storedHits, _apiCalls, _missing, _storeErrors);
    }

    private void StoreError(Exception ex, string action, string url)
    {
        // Only the first few, so a missing table doesn't flood the log
        if (Interlocked.Increment(ref _storeErrors) <= 5)
            _logger.LogWarning(ex, "Couldn't {Action} the raw response for {Url}; carrying on without the store", action, url);
    }

    [GeneratedRegex(@"(?<!\d)20\d{8}(?!\d)")]
    private static partial Regex GameIdRegex();
}

/// <summary>A stored endpoint had no stored copy while the run may only use stored copies (RUN_MODE=Reparse).</summary>
public class StoredResponseMissingException(string url) : Exception($"No stored response for {url}");