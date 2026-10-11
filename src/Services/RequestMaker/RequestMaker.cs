using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Services.NhlData;

namespace Services.RequestMaker;

public class RequestMaker : IRequestMaker
{
    private readonly IHttpClient _client;
    private readonly ILogger<RequestMaker> _logger;
    private readonly Dictionary<string, JsonNode> _cachedResponses = new Dictionary<string, JsonNode>();
    private int _cacheSize;
    private const int _cacheByteSizeLimit = 1000000000; // 1 GB cache size limit
    private DateTime _lastRequestCompleted = DateTime.MinValue;
    private readonly int _throttleTimeMs;
    private readonly RawResponseCache? _rawResponses;

    /// <param name="rawResponses">Stores (and per its policy, serves) the raw text of the endpoints worth keeping</param>
    public RequestMaker(IHttpClient client, ILoggerFactory loggerFactory, int throttleTimeMs, RawResponseCache? rawResponses = null)
    {
        _client = client;
        _logger = loggerFactory.CreateLogger<RequestMaker>();
        _throttleTimeMs = throttleTimeMs;
        _rawResponses = rawResponses;
    }
    /// <summary>
    /// Given a url and query makes a request and returns the response converted to a dynamic object
    /// </summary>
    /// <param name="url">Base url to call</param>
    /// <param name="query">Query parameters to append to url</param>
    /// <returns>Dynamic response object</returns>
    public async Task<JsonNode?> MakeRequest(string url, string query)
    {
        return await MakeRequest(url, query, _throttleTimeMs);
    }
    /// <summary>
    /// Given a url and query makes a request and returns the response converted to a dynamic object
    /// </summary>
    /// <param name="url">Base url to call</param>
    /// <param name="query">Query parameters to append to url</param>
    /// <param name="throttleTimeMs">How much time should elapse before making another request</param>
    /// <returns>Dynamic response object</returns>
    public async Task<JsonNode?> MakeRequest(string url, string query, int throttleTimeMs)
    {
        string key = url + query;
        if (_cachedResponses.ContainsKey(key))
        {
            _logger.LogInformation("Returning cached response for: " + key);
            return _cachedResponses[key];
        }

        string? data;
        try
        {
            var fullUrl = url + query;
            (_, data) = _rawResponses != null
                ? await _rawResponses.Get(fullUrl, () => Send(fullUrl, throttleTimeMs))
                : await Send(fullUrl, throttleTimeMs);
        }
        catch (StoredResponseMissingException ex)
        {
            _logger.LogWarning("{Message}", ex.Message);
            return null;
        }

        var serviceResponse = data == null ? null : JsonNode.Parse(data);
        AddToCache(key, serviceResponse);

        return serviceResponse;
    }

    /// <summary>
    /// Sends the request (throttled) and returns the status and body (null unless it succeeded). A 429 is retried with
    /// growing waits, like <see cref="ThrottledHttpClient"/>.
    /// </summary>
    private async Task<(HttpStatusCode Status, string? Body)> Send(string requestUrl, int throttleTimeMs)
    {
        for (var attempt = 1; ; attempt++)
        {
            await ThrottleRequest(throttleTimeMs);

            HttpRequestMessage msg = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(requestUrl),
            };
            msg.Headers.Accept.Clear();
            msg.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await _client.SendAsync(msg);
            _lastRequestCompleted = DateTime.UtcNow;

            if (response.StatusCode == HttpStatusCode.TooManyRequests && attempt <= ThrottledHttpClient.RATE_LIMIT_WAITS.Length)
            {
                var wait = ThrottledHttpClient.RetryWait(response, ThrottledHttpClient.RATE_LIMIT_WAITS[attempt - 1]);
                _logger.LogWarning("Rate limited (429) on {Url}; retrying in {Wait}", requestUrl, wait);
                await Task.Delay(wait);
                continue;
            }

            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Request failed with {StatusCode} for {Url}: {Body}",
                    (int)response.StatusCode, requestUrl, body.Length > 300 ? body[..300] : body);
                return (response.StatusCode, null);
            }
            return (response.StatusCode, body);
        }
    }

    /// <summary>
    /// Time to throttle for
    /// </summary>
    /// <param name="throttleTimeMs">Milliseconds that should pass before making a request</param>
    private async Task ThrottleRequest(int? throttleTimeMs)
    {
        // Throttling logic
        if (throttleTimeMs.HasValue && _lastRequestCompleted != DateTime.MinValue)
        {
            var elapsed = DateTime.UtcNow - _lastRequestCompleted;
            var waitMs = throttleTimeMs.Value - (int)elapsed.TotalMilliseconds;
            if (waitMs > 0)
            {
                await Task.Delay(waitMs);
            }
        }
    }

    /// <summary>
    /// Adds a response to the cache.
    /// </summary>
    /// <param name="key">The request</param>
    /// <param name="jsonResponse">The response to cache</param>
    private void AddToCache(string key, JsonNode? serviceResponse)
    {
        if (serviceResponse != null)
        {
            _cachedResponses[key] = serviceResponse;
            string responseString = serviceResponse.ToJsonString();
            _cacheSize += Encoding.UTF8.GetByteCount(responseString);
            if (_cacheSize > _cacheByteSizeLimit)
            {
                _logger.LogWarning("Cache size exceeded limit of 1 GB. Clearing cache.");
                _cachedResponses.Clear();
                _cacheSize = 0;
            }
        }
    }
}