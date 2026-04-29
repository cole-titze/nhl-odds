using System.Net;
using System.Text.Json;
using Entities.ServiceModels.OddsApi;
using Microsoft.Extensions.Logging;

namespace Services.OddsApi;

public class OddsApiGetter : IOddsApiGetter
{
    private const string BASE_URL = "https://api.the-odds-api.com/v4/sports/icehockey_nhl/odds";
    private const string HISTORICAL_BASE_URL = "https://api.the-odds-api.com/v4/historical/sports/icehockey_nhl/odds";
    private readonly string _apiKey;
    private readonly HttpClient _httpClient;
    private readonly ILogger<OddsApiGetter> _logger;
    private int? _remainingRequests;

    public OddsApiGetter(string apiKey, ILoggerFactory loggerFactory)
    {
        _apiKey = apiKey;
        _httpClient = new HttpClient();
        _logger = loggerFactory.CreateLogger<OddsApiGetter>();
    }

    public async Task<OddsApiResult> GetUpcomingOdds()
    {
        var query = $"?apiKey={_apiKey}&regions=us&markets=h2h,spreads,totals&oddsFormat=american";
        var url = BASE_URL + query;

        try
        {
            var response = await _httpClient.GetAsync(url);
            ReadRemainingRequests(response);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
                throw new OddsApiRateLimitException("Odds API rate limit reached (429)");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Odds API request failed with status {StatusCode}", response.StatusCode);
                return new OddsApiResult();
            }

            var json = await response.Content.ReadAsStringAsync();
            var results = JsonSerializer.Deserialize<List<OddsApiResponse>>(json);
            return new OddsApiResult
            {
                Responses = results ?? new List<OddsApiResponse>(),
                RawJson = json,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch odds from The Odds API");
            return new OddsApiResult();
        }
    }

    public async Task<OddsApiResult> GetHistoricalOdds(DateTime date)
    {
        var dateStr = date.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var query = $"?apiKey={_apiKey}&regions=us&markets=h2h,spreads,totals&oddsFormat=american&date={dateStr}";
        var url = HISTORICAL_BASE_URL + query;

        try
        {
            var response = await _httpClient.GetAsync(url);
            ReadRemainingRequests(response);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
                throw new OddsApiRateLimitException("Odds API rate limit reached (429)");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Historical Odds API request failed with status {StatusCode} for date {Date}",
                    response.StatusCode, dateStr);
                return new OddsApiResult();
            }

            var json = await response.Content.ReadAsStringAsync();
            var wrapper = JsonSerializer.Deserialize<OddsApiHistoricalResponse>(json);
            return new OddsApiResult
            {
                Responses = wrapper?.Data ?? new List<OddsApiResponse>(),
                RawJson = json,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch historical odds for date {Date}", dateStr);
            return new OddsApiResult();
        }
    }

    public int? GetRemainingRequests() => _remainingRequests;

    private void ReadRemainingRequests(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("x-requests-remaining", out var remainingValues))
        {
            var remainingStr = remainingValues.FirstOrDefault();
            if (int.TryParse(remainingStr, out var remaining))
            {
                _remainingRequests = remaining;
                _logger.LogInformation("Odds API requests remaining: {Remaining}", remaining);
            }
        }
    }
}