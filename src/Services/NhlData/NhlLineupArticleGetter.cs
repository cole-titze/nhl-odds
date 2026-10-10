using Microsoft.Extensions.Logging;

namespace Services.NhlData;

public class NhlLineupArticleGetter : INhlLineupArticleGetter
{
    private const string ARTICLE_URL =
        "https://forge-dapi.d3.nhle.com/v2/content/en-us/stories/nhl-lineup-projections-2026-27-season";
    private readonly HttpClient _httpClient;
    private readonly ILogger<NhlLineupArticleGetter> _logger;

    public NhlLineupArticleGetter(ILoggerFactory loggerFactory)
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        _logger = loggerFactory.CreateLogger<NhlLineupArticleGetter>();
    }

    public async Task<string?> GetLineupArticle()
    {
        try
        {
            return await _httpClient.GetStringAsync(ARTICLE_URL);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch the NHL lineup projections article");
            return null;
        }
    }
}