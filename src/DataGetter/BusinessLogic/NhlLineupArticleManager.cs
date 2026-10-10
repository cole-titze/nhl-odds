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
/// The article is overwritten daily, so these snapshots are the only history of what it said before games. A new
/// article starts each season (<see cref="NhlSeasonArticles"/>).
/// </summary>
public class NhlLineupArticleManager
{
    private readonly ILineupArticleRepository _lineupArticleRepo;
    private readonly INhlContentGetter _contentGetter;
    private readonly ILogger<NhlLineupArticleManager> _logger;

    public NhlLineupArticleManager(ILineupArticleRepository lineupArticleRepository, INhlContentGetter contentGetter, ILoggerFactory loggerFactory)
    {
        _lineupArticleRepo = lineupArticleRepository;
        _contentGetter = contentGetter;
        _logger = loggerFactory.CreateLogger<NhlLineupArticleManager>();
    }

    public async Task SaveLineupArticle(DateTime nowUtc)
    {
        var found = await NhlSeasonArticles.FindLineupArticle(_contentGetter, nowUtc)
            ?? throw new Exception($"No lineup projections article for {NhlSeasonArticles.Season(nowUtc)} or the season before");
        var article = ParseArticle(found.Json, nowUtc.AddTicks(-(nowUtc.Ticks % TimeSpan.TicksPerSecond)));

        if (await _lineupArticleRepo.Exists(article.ContentHash))
        {
            _logger.LogInformation("Lineup article {Slug} unchanged (last updated {LastUpdated})", found.Slug, article.LastUpdated);
        }
        else
        {
            await _lineupArticleRepo.Add(article);
            _logger.LogInformation("Saved new lineup article version {Slug} (last updated {LastUpdated})", found.Slug, article.LastUpdated);
        }

        if (!found.CurrentSeason && NhlSeasonArticles.SeasonUnderway(nowUtc))
            throw new Exception($"Still reading last season's lineup article {found.Slug}: no {NhlSeasonArticles.Season(nowUtc)} article found");
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