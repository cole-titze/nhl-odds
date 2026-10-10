namespace Services.NhlData;

public class NhlContentGetter : INhlContentGetter
{
    private const string STORIES_URL = "https://forge-dapi.d3.nhle.com/v2/content/en-us/stories";
    private const string PARTNER_ODDS_URL = "https://api-web.nhle.com/v1/partner-game";
    private readonly HttpClient _httpClient;

    public NhlContentGetter()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
    }

    public Task<string> GetLatestStories(int limit)
    {
        return _httpClient.GetStringAsync($"{STORIES_URL}?$limit={limit}");
    }

    public Task<string> GetStoryByUrl(string selfUrl)
    {
        if (!selfUrl.StartsWith(STORIES_URL + "/"))
            throw new ArgumentException($"Unexpected article URL: {selfUrl}", nameof(selfUrl));
        return _httpClient.GetStringAsync(selfUrl);
    }

    public Task<string> GetStoryBySlug(string slug)
    {
        return _httpClient.GetStringAsync($"{STORIES_URL}/{Uri.EscapeDataString(slug)}");
    }

    public Task<string> GetPartnerOdds(string country)
    {
        return _httpClient.GetStringAsync($"{PARTNER_ODDS_URL}/{country}/now");
    }
}