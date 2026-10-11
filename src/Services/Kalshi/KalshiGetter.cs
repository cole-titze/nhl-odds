using System.Net;
using System.Text.Json;
using Entities.ServiceModels.Kalshi;
using Microsoft.Extensions.Logging;

namespace Services.Kalshi;

public class KalshiGetter : IKalshiGetter
{
    private const string BASE_URL = "https://api.elections.kalshi.com/trade-api/v2";
    private const int THROTTLE_MS = 250;
    private const int MAX_RATE_LIMIT_RETRIES = 5;
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
        return await FetchMarketsPaginated($"/markets?series_ticker={seriesTicker}&status=open&limit=1000", seriesTicker,
            throwOnFailure: true);
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

    public async Task<List<KalshiMarket>> GetEventMarkets(string eventTicker)
    {
        var markets = await FetchMarketsPaginated($"/markets?event_ticker={eventTicker}&limit=1000", eventTicker);
        return markets.Count > 0
            ? markets
            : await FetchMarketsPaginated($"/historical/markets?event_ticker={eventTicker}&limit=1000", eventTicker);
    }

    public async Task<KalshiCandlestickResponse?> GetCandlesticks(string seriesTicker, string ticker, long startTs, long endTs, int periodInterval)
    {
        var query = $"candlesticks?start_ts={startTs}&end_ts={endTs}&period_interval={periodInterval}";
        // Markets settled before Kalshi's historical cutoff only answer on the historical endpoint
        return await FetchCandlesticks($"{BASE_URL}/series/{seriesTicker}/markets/{ticker}/{query}", ticker)
            ?? await FetchCandlesticks($"{BASE_URL}/historical/markets/{ticker}/{query}", ticker);
    }

    private async Task<KalshiCandlestickResponse?> FetchCandlesticks(string url, string ticker)
    {
        try
        {
            var response = await GetWithRetry(url);

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

    /// <param name="throwOnFailure">Throw instead of returning what was fetched so far (the daily fetch fails the job
    /// and logs to ErrorLog; backfills carry on)</param>
    private async Task<List<KalshiMarket>> FetchMarketsPaginated(string basePath, string seriesTicker, bool throwOnFailure = false)
    {
        var allMarkets = new List<KalshiMarket>();
        var cursor = "";

        try
        {
            do
            {
                var url = BASE_URL + basePath;
                if (!string.IsNullOrEmpty(cursor))
                    url += $"&cursor={cursor}";

                var response = await GetWithRetry(url);

                if (!response.IsSuccessStatusCode)
                {
                    if (throwOnFailure)
                        throw new HttpRequestException(
                            $"Kalshi API request failed with status {(int)response.StatusCode} for {seriesTicker}", null, response.StatusCode);
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
        catch (Exception ex) when (!throwOnFailure)
        {
            _logger.LogError(ex, "Failed to fetch Kalshi markets for {Ticker}", seriesTicker);
        }

        _logger.LogInformation("Fetched {Count} Kalshi markets for {Ticker}", allMarkets.Count, seriesTicker);
        return allMarkets;
    }

    /// <summary>GET, waiting 1, 2, 4, 8 then 16 s on each 429 (long backfills run into Kalshi's rate limit)</summary>
    private async Task<HttpResponseMessage> GetWithRetry(string url)
    {
        for (var attempt = 0; ; attempt++)
        {
            await Throttle();
            var response = await _httpClient.GetAsync(url);
            _lastRequestCompleted = DateTime.UtcNow;
            if (response.StatusCode != HttpStatusCode.TooManyRequests || attempt >= MAX_RATE_LIMIT_RETRIES)
                return response;

            var wait = TimeSpan.FromSeconds(Math.Pow(2, attempt));
            _logger.LogInformation("Kalshi rate limit hit; retrying in {Seconds} s", wait.TotalSeconds);
            await Task.Delay(wait);
        }
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