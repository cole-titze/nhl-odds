using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Services.RequestMaker;

public class RequestMaker : IRequestMaker
{
    private readonly IHttpClient _client;
    private readonly ILogger<RequestMaker> _logger;
    private readonly Dictionary<string, dynamic> _cachedResponses = new Dictionary<string, dynamic>();
    private int _cacheSize;
    private const int _cacheByteSizeLimit = 1000000000; // 1 GB cache size limit
    private DateTime _lastRequestCompleted = DateTime.MinValue;
    private readonly int _throttleTimeMs;

    public RequestMaker(IHttpClient client, ILoggerFactory loggerFactory, int throttleTimeMs)
    {
        _client = client;
        _logger = loggerFactory.CreateLogger<RequestMaker>();
        _throttleTimeMs = throttleTimeMs;
    }
    /// <summary>
    /// Given a url and query makes a request and returns the response converted to a dynamic object
    /// </summary>
    /// <param name="url">Base url to call</param>
    /// <param name="query">Query parameters to append to url</param>
    /// <returns>Dynamic response object</returns>
    public async Task<dynamic?> MakeRequest(string url, string query)
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
    public async Task<dynamic?> MakeRequest(string url, string query, int throttleTimeMs)
    {
        string key = url + query;
        if (_cachedResponses.ContainsKey(key))
            return _cachedResponses[key];

        await ThrottleRequest(throttleTimeMs);

        HttpResponseMessage response;
        HttpRequestMessage msg = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(url + query),
        };
        msg.Headers.Accept.Clear();
        msg.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        response = await _client.SendAsync(msg);
        _lastRequestCompleted = DateTime.UtcNow;

        var serviceResponse = await ParseResponse(response);
        AddToCache(key, serviceResponse);

        return serviceResponse;
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
    private void AddToCache(string key, dynamic? serviceResponse)
    {
        if (serviceResponse != null)
        {
            _cachedResponses[key] = serviceResponse;
            string responseString = JsonConvert.SerializeObject(serviceResponse);
            _cacheSize += Encoding.UTF8.GetByteCount(responseString);
            if (_cacheSize > _cacheByteSizeLimit)
            {
                _logger.LogWarning("Cache size exceeded limit of 1 GB. Clearing cache.");
                _cachedResponses.Clear();
                _cacheSize = 0;
            }
        }
    }

    /// <summary>
    /// Converts the raw http response into a dynamic object.
    /// </summary>
    /// <param name="response">The raw http response message</param>
    /// <returns>Dynamic object</returns>
    private static async Task<dynamic?> ParseResponse(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
            return null;

        // Get data as Json string 
        string data = await response.Content.ReadAsStringAsync();
        // Add Json string conversion to hard object
        var message = JsonConvert.DeserializeObject<dynamic>(data);

        return message;
    }
}

