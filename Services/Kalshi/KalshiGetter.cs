using Entities.ServiceModels.Kalshi;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Services.Kalshi;

public class KalshiGetter : IKalshiGetter
{
    private const string BASE_URL = "https://api.elections.kalshi.com/trade-api/v2";
    private const int THROTTLE_MS = 100;
    private readonly HttpClient _httpClient;
    private readonly ILogger<KalshiGetter> _logger;
    private DateTime _lastRequestCompleted = DateTime.MinValue;

    public KalshiGetter(ILoggerFactory loggerFactory)
    {
        _httpClient = new HttpClient();
        _logger = loggerFactory.CreateLogger<KalshiGetter>();
    }

    public async Task<List<KalshiMarket>> GetOpenMarkets(string seriesTicker)
    {
        var allMarkets = new List<KalshiMarket>();
        var cursor = "";

        try
        {
            do
            {
                await Throttle();
                var query = $"?series_ticker={seriesTicker}&status=open&limit=1000";
                if (!string.IsNullOrEmpty(cursor))
                    query += $"&cursor={cursor}";

                var url = BASE_URL + "/markets" + query;
                var response = await _httpClient.GetAsync(url);
                _lastRequestCompleted = DateTime.UtcNow;

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Kalshi API request failed with status {StatusCode} for {Ticker}",
                        response.StatusCode, seriesTicker);
                    break;
                }

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<KalshiMarketsResponse>(json);

                if (result?.Markets == null || result.Markets.Count == 0)
                    break;

                allMarkets.AddRange(result.Markets);
                cursor = result.Cursor;
            } while (!string.IsNullOrEmpty(cursor));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch Kalshi markets for {Ticker}", seriesTicker);
        }

        _logger.LogInformation("Fetched {Count} Kalshi markets for {Ticker}", allMarkets.Count, seriesTicker);
        return allMarkets;
    }

    private async Task Throttle()
    {
        if (_lastRequestCompleted != DateTime.MinValue)
        {
            var elapsed = DateTime.UtcNow - _lastRequestCompleted;
            var waitMs = THROTTLE_MS - (int)elapsed.TotalMilliseconds;
            if (waitMs > 0)
                await Task.Delay(waitMs);
        }
    }
}
