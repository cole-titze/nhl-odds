namespace Services.NhlData;

public class NhlContentGetter : INhlContentGetter
{
    private const string STORIES_URL = "https://forge-dapi.d3.nhle.com/v2/content/en-us/stories";
    private const string PARTNER_ODDS_URL = "https://api-web.nhle.com/v1/partner-game";
    private const string ROSTER_URL = "https://api-web.nhle.com/v1/roster";
    private readonly ThrottledHttpClient _apiWeb = new(ThrottledHttpClient.NHL_MIN_GAP);
    private readonly ThrottledHttpClient _forge = new(ThrottledHttpClient.NHL_MIN_GAP);

    public Task<string> GetLatestStories(int limit)
    {
        return _forge.GetString($"{STORIES_URL}?$limit={limit}");
    }

    public Task<string> GetStoriesByTags(IEnumerable<string> tags, int limit)
    {
        var filter = string.Concat(tags.Select(t => $"tags.slug={Uri.EscapeDataString(t)}&"));
        return _forge.GetString($"{STORIES_URL}?{filter}$limit={limit}");
    }

    public Task<string> GetStoryByUrl(string selfUrl)
    {
        if (!selfUrl.StartsWith(STORIES_URL + "/"))
            throw new ArgumentException($"Unexpected article URL: {selfUrl}", nameof(selfUrl));
        return _forge.GetString(selfUrl);
    }

    public Task<string> GetStoryBySlug(string slug)
    {
        return _forge.GetString($"{STORIES_URL}/{Uri.EscapeDataString(slug)}");
    }

    public Task<string> GetPartnerOdds(string country)
    {
        return _apiWeb.GetString($"{PARTNER_ODDS_URL}/{country}/now");
    }

    public Task<string> GetTeamRoster(string teamAbbreviation)
    {
        return _apiWeb.GetString($"{ROSTER_URL}/{Uri.EscapeDataString(teamAbbreviation)}/current");
    }
}