using System.Net;

namespace Services.NhlData;

/// <summary>
/// An HttpClient that spaces requests at least <c>minGap</c> apart and retries a 429 with growing waits. Use one per host
/// for the whole run. The NHL also rate limits per IP across its hosts, so another job running at the same time (e.g.
/// the lineup snapshot during a backfill) can still push it over; the retries ride that out.
/// </summary>
public class ThrottledHttpClient
{
    // Waits before each retry after a 429, about 2.5 minutes in total, unless the response asks for a specific wait
    public static readonly TimeSpan[] RATE_LIMIT_WAITS =
        [TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(40), TimeSpan.FromSeconds(80)];
    private static readonly TimeSpan MAX_RETRY_AFTER = TimeSpan.FromMinutes(2);
    private readonly HttpClient _httpClient;
    private readonly TimeSpan _minGap;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private DateTime _lastRequestUtc = DateTime.MinValue;

    public ThrottledHttpClient(TimeSpan minGap, IDictionary<string, string>? headers = null)
    {
        _minGap = minGap;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        foreach (var (name, value) in headers ?? new Dictionary<string, string>())
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(name, value);
    }

    public async Task<string> GetString(string url)
    {
        var (status, body) = await Get(url);
        if (status != HttpStatusCode.OK)
            throw new HttpRequestException($"{url} returned {(int)status} ({status})", null, status);
        return body!;
    }

    /// <summary>The status and body (null unless 200), without throwing on an error status.</summary>
    public async Task<(HttpStatusCode Status, string? Body)> Get(string url)
    {
        for (var attempt = 1; ; attempt++)
        {
            await _gate.WaitAsync();
            try
            {
                var wait = _lastRequestUtc + _minGap - DateTime.UtcNow;
                if (wait > TimeSpan.Zero)
                    await Task.Delay(wait);
                _lastRequestUtc = DateTime.UtcNow;
            }
            finally
            {
                _gate.Release();
            }

            using var response = await _httpClient.GetAsync(url);
            if (response.StatusCode == HttpStatusCode.TooManyRequests && attempt <= RATE_LIMIT_WAITS.Length)
            {
                await Task.Delay(RetryWait(response, RATE_LIMIT_WAITS[attempt - 1]));
                continue;
            }
            return response.IsSuccessStatusCode
                ? (response.StatusCode, await response.Content.ReadAsStringAsync())
                : (response.StatusCode, null);
        }
    }

    // The server's Retry-After when it sends one (capped), otherwise the default wait
    public static TimeSpan RetryWait(HttpResponseMessage response, TimeSpan defaultWait)
    {
        var retryAfter = response.Headers.RetryAfter;
        var wait = retryAfter?.Delta ?? (retryAfter?.Date - DateTimeOffset.UtcNow);
        if (wait is not { } w || w <= TimeSpan.Zero)
            return defaultWait;
        return w < MAX_RETRY_AFTER ? w : MAX_RETRY_AFTER;
    }
}