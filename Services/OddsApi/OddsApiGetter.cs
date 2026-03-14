using Entities.ServiceModels.OddsApi;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Services.OddsApi;

public class OddsApiGetter : IOddsApiGetter
{
    private const string BASE_URL = "https://api.the-odds-api.com/v4/sports/icehockey_nhl/odds";
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
        var query = $"?apiKey={_apiKey}&regions=us&markets=h2h&oddsFormat=american";
        var url = BASE_URL + query;

        try
        {
            var response = await _httpClient.GetAsync(url);

            if (response.Headers.TryGetValues("x-requests-remaining", out var remainingValues))
            {
                var remainingStr = remainingValues.FirstOrDefault();
                if (int.TryParse(remainingStr, out var remaining))
                {
                    _remainingRequests = remaining;
                    _logger.LogInformation("Odds API requests remaining: {Remaining}", remaining);
                }
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Odds API request failed with status {StatusCode}", response.StatusCode);
                return new OddsApiResult();
            }

            var json = await response.Content.ReadAsStringAsync();
            var results = JsonConvert.DeserializeObject<List<OddsApiResponse>>(json);
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

    public int? GetRemainingRequests() => _remainingRequests;
}
