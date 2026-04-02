using System.Text.Json;
using Entities.ServiceModels.Kalshi;
using Microsoft.Extensions.Logging;

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
        return await FetchMarketsPaginated($"/markets?series_ticker={seriesTicker}&status=open&limit=1000", seriesTicker);
    }

    public async Task<KalshiCutoffResponse?> GetCutoff()
    {
        try
        {
            await Throttle();
            var response = await _httpClient.GetAsync(BASE_URL + "/historical/cutoff");
            _lastRequestCompleted = DateTime.UtcNow;

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Kalshi cutoff request failed with status {StatusCode}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<KalshiCutoffResponse>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch Kalshi cutoff");
            return null;
        }
    }

    public async Task<List<KalshiMarket>> GetSettledMarkets(string seriesTicker)
    {
        return await FetchMarketsPaginated($"/markets?series_ticker={seriesTicker}&status=settled&limit=1000", seriesTicker);
    }

    public async Task<List<KalshiMarket>> GetHistoricalMarkets(string seriesTicker)
    {
        return await FetchMarketsPaginated($"/historical/markets?series_ticker={seriesTicker}&limit=1000", seriesTicker);
    }

    public async Task<KalshiCandlestickResponse?> GetCandlesticks(string seriesTicker, string ticker, long startTs, long endTs, int periodInterval)
    {
        try
        {
            await Throttle();
            var url = $"{BASE_URL}/series/{seriesTicker}/markets/{ticker}/candlesticks?start_ts={startTs}&end_ts={endTs}&period_interval={periodInterval}";
            var response = await _httpClient.GetAsync(url);
            _lastRequestCompleted = DateTime.UtcNow;

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<KalshiCandlestickResponse>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch candlesticks for {Ticker}", ticker);
            return null;
        }
    }

    private async Task<List<KalshiMarket>> FetchMarketsPaginated(string basePath, string seriesTicker)
    {
        var allMarkets = new List<KalshiMarket>();
        var cursor = "";

        try
        {
            do
            {
                await Throttle();
                var url = BASE_URL + basePath;
                if (!string.IsNullOrEmpty(cursor))
                    url += $"&cursor={cursor}";

                var response = await _httpClient.GetAsync(url);
                _lastRequestCompleted = DateTime.UtcNow;

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Kalshi API request failed with status {StatusCode} for {Ticker}",
                        response.StatusCode, seriesTicker);
                    break;
                }

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<KalshiMarketsResponse>(json);

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
