using System.Net;

namespace Services.NhlData;

public class NhlGameDetailGetter : INhlGameDetailGetter
{
    private const string PLAY_BY_PLAY_URL = "https://api-web.nhle.com/v1/gamecenter";
    private const string SHIFT_CHART_URL = "https://api.nhle.com/stats/rest/en/shiftcharts";
    private const string REPLAY_URL = "https://wsr.nhle.com/sprites/";
    // One client per host, each at the NHL gap, so their requests can run side by side
    private readonly ThrottledHttpClient _apiWeb = new(ThrottledHttpClient.NHL_MIN_GAP);
    private readonly ThrottledHttpClient _stats = new(ThrottledHttpClient.NHL_MIN_GAP);
    // The replay files are refused (403) without a browser-like user agent and referer
    private readonly ThrottledHttpClient _replays = new(ThrottledHttpClient.NHL_MIN_GAP, new Dictionary<string, string>
    {
        ["User-Agent"] = "Mozilla/5.0",
        ["Referer"] = "https://www.nhl.com/",
    });

    private readonly RawResponseCache? _rawResponses;

    /// <param name="rawResponses">Stores (and per its policy, serves) the raw responses</param>
    public NhlGameDetailGetter(RawResponseCache? rawResponses = null)
    {
        _rawResponses = rawResponses;
    }

    /// <summary>A gamecenter endpoint's URL ("boxscore", "right-rail", "play-by-play"), as the game fetch builds it too.</summary>
    public static string GameCenterUrl(int gameId, string endpoint) => $"{PLAY_BY_PLAY_URL}/{gameId}/{endpoint}";

    public static string ShiftChartUrl(int gameId) => $"{SHIFT_CHART_URL}?cayenneExp=gameId={gameId}";

    public Task<string> GetPlayByPlay(int gameId)
    {
        return GetString(_apiWeb, GameCenterUrl(gameId, "play-by-play"));
    }

    public Task<string> GetShiftChart(int gameId)
    {
        return GetString(_stats, ShiftChartUrl(gameId));
    }

    public Task<(HttpStatusCode Status, string? Body)> GetGoalReplay(string url)
    {
        if (!url.StartsWith(REPLAY_URL))
            throw new ArgumentException($"Unexpected replay URL: {url}", nameof(url));
        return Get(_replays, url);
    }

    private Task<(HttpStatusCode Status, string? Body)> Get(ThrottledHttpClient client, string url)
    {
        return _rawResponses != null ? _rawResponses.Get(url, () => client.Get(url)) : client.Get(url);
    }

    private async Task<string> GetString(ThrottledHttpClient client, string url)
    {
        var (status, body) = await Get(client, url);
        if (status != HttpStatusCode.OK)
            throw new HttpRequestException($"{url} returned {(int)status} ({status})", null, status);
        return body!;
    }
}