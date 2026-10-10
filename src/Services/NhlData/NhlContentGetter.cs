using System.Net;

namespace Services.NhlData;

public class NhlContentGetter : INhlContentGetter
{
    private const string STORIES_URL = "https://forge-dapi.d3.nhle.com/v2/content/en-us/stories";
    private const string PARTNER_ODDS_URL = "https://api-web.nhle.com/v1/partner-game";
    private const string ROSTER_URL = "https://api-web.nhle.com/v1/roster";
    // api-web answers 429 when requests come faster than about one every 500 ms
    private static readonly TimeSpan MIN_REQUEST_GAP = TimeSpan.FromMilliseconds(600);
    private static readonly TimeSpan RATE_LIMIT_WAIT = TimeSpan.FromSeconds(10);
    private readonly HttpClient _httpClient;
    private DateTime _lastRequestUtc = DateTime.MinValue;

    public NhlContentGetter()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
    }

    public Task<string> GetLatestStories(int limit)
    {
        return Get($"{STORIES_URL}?$limit={limit}");
    }

    public Task<string> GetStoryByUrl(string selfUrl)
    {
        if (!selfUrl.StartsWith(STORIES_URL + "/"))
            throw new ArgumentException($"Unexpected article URL: {selfUrl}", nameof(selfUrl));
        return Get(selfUrl);
    }

    public Task<string> GetStoryBySlug(string slug)
    {
        return Get($"{STORIES_URL}/{Uri.EscapeDataString(slug)}");
    }

    public Task<string> GetPartnerOdds(string country)
    {
        return Get($"{PARTNER_ODDS_URL}/{country}/now");
    }

    public Task<string> GetTeamRoster(string teamAbbreviation)
    {
        return Get($"{ROSTER_URL}/{Uri.EscapeDataString(teamAbbreviation)}/current");
    }

    /// <summary>Spaces requests out and retries once after a 429.</summary>
    private async Task<string> Get(string url)
    {
        for (var attempt = 1; ; attempt++)
        {
            var wait = _lastRequestUtc + MIN_REQUEST_GAP - DateTime.UtcNow;
            if (wait > TimeSpan.Zero)
                await Task.Delay(wait);
            _lastRequestUtc = DateTime.UtcNow;
            try
            {
                return await _httpClient.GetStringAsync(url);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests && attempt == 1)
            {
                await Task.Delay(RATE_LIMIT_WAIT);
            }
        }
    }
}