using System.Net;

namespace Services.NhlData;

public class NhlGameDetailGetter : INhlGameDetailGetter
{
    private const string PLAY_BY_PLAY_URL = "https://api-web.nhle.com/v1/gamecenter";
    private const string SHIFT_CHART_URL = "https://api.nhle.com/stats/rest/en/shiftcharts";
    private const string REPLAY_URL = "https://wsr.nhle.com/sprites/";
    // One client per host: each rate limits separately, so their requests can run side by side
    private readonly ThrottledHttpClient _apiWeb = new(TimeSpan.FromMilliseconds(600));
    private readonly ThrottledHttpClient _stats = new(TimeSpan.FromMilliseconds(500));
    // The replay files are refused (403) without a browser-like user agent and referer
    private readonly ThrottledHttpClient _replays = new(TimeSpan.FromMilliseconds(250), new Dictionary<string, string>
    {
        ["User-Agent"] = "Mozilla/5.0",
        ["Referer"] = "https://www.nhl.com/",
    });

    public Task<string> GetPlayByPlay(int gameId)
    {
        return _apiWeb.GetString($"{PLAY_BY_PLAY_URL}/{gameId}/play-by-play");
    }

    public Task<string> GetShiftChart(int gameId)
    {
        return _stats.GetString($"{SHIFT_CHART_URL}?cayenneExp=gameId={gameId}");
    }

    public Task<(HttpStatusCode Status, string? Body)> GetGoalReplay(string url)
    {
        if (!url.StartsWith(REPLAY_URL))
            throw new ArgumentException($"Unexpected replay URL: {url}", nameof(url));
        return _replays.Get(url);
    }
}