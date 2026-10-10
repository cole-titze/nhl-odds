using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DatabaseAccess.LineupArticleRepository;
using Entities.DbModels;
using Microsoft.Extensions.Logging;
using Services.NhlData;

namespace DataGetter.BusinessLogic;

/// <summary>
/// Saves each new version of NHL.com's lineup projections article (projected lines, starting goalies, scratches).
/// The article is overwritten daily, so these snapshots are the only history of what it said before games.
/// </summary>
public class NhlLineupArticleManager
{
    private readonly ILineupArticleRepository _lineupArticleRepo;
    private readonly INhlLineupArticleGetter _articleGetter;
    private readonly ILogger<NhlLineupArticleManager> _logger;

    public NhlLineupArticleManager(ILineupArticleRepository lineupArticleRepository, INhlLineupArticleGetter articleGetter, ILoggerFactory loggerFactory)
    {
        _lineupArticleRepo = lineupArticleRepository;
        _articleGetter = articleGetter;
        _logger = loggerFactory.CreateLogger<NhlLineupArticleManager>();
    }

    public async Task SaveLineupArticle(DateTime nowUtc)
    {
        var json = await _articleGetter.GetLineupArticle()
            ?? throw new Exception("Lineup projections article couldn't be fetched");
        var article = ParseArticle(json, nowUtc.AddTicks(-(nowUtc.Ticks % TimeSpan.TicksPerSecond)));

        if (await _lineupArticleRepo.Exists(article.ContentHash))
        {
            _logger.LogInformation("Lineup article unchanged (last updated {LastUpdated})", article.LastUpdated);
            return;
        }
        await _lineupArticleRepo.Add(article);
        _logger.LogInformation("Saved new lineup article version (last updated {LastUpdated})", article.LastUpdated);
    }

    /// <summary>
    /// Reads the article's dates and hashes its text: the markdown parts joined with "\n". Metadata (editor,
    /// photos, timestamps) is left out of the hash so a save that doesn't change the text isn't a new version.
    /// </summary>
    public static DbLineupArticle ParseArticle(string json, DateTime firstSeenUtc)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var text = string.Join("\n", root.GetProperty("parts").EnumerateArray()
            .Where(p => p.TryGetProperty("type", out var type) && type.GetString() == "markdown"
                && p.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.String)
            .Select(p => p.GetProperty("content").GetString()));
        if (text.Length == 0)
            throw new Exception("Lineup projections article has no text");

        return new DbLineupArticle
        {
            ContentHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text))),
            FirstSeenUTC = firstSeenUtc,
            ContentDate = ReadDate(root, "contentDate"),
            LastUpdated = ReadDate(root, "lastUpdatedDate"),
            RawJson = json,
        };
    }

    private static DateTime? ReadDate(JsonElement root, string name)
    {
        return root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            && DateTime.TryParse(value.GetString(), null, System.Globalization.DateTimeStyles.AdjustToUniversal, out var date)
            ? date
            : null;
    }
}