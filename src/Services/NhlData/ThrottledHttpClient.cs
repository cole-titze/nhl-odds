using System.Net;

namespace Services.NhlData;

/// <summary>
/// An HttpClient that spaces requests at least <c>minGap</c> apart and retries once after a 429. Use one per host for
/// the whole run: the NHL's hosts rate limit separately.
/// </summary>
public class ThrottledHttpClient
{
    private static readonly TimeSpan RATE_LIMIT_WAIT = TimeSpan.FromSeconds(10);
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
            if (response.StatusCode == HttpStatusCode.TooManyRequests && attempt == 1)
            {
                await Task.Delay(RATE_LIMIT_WAIT);
                continue;
            }
            return response.IsSuccessStatusCode
                ? (response.StatusCode, await response.Content.ReadAsStringAsync())
                : (response.StatusCode, null);
        }
    }
}