using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DatabaseAccess.SnapshotRepository;
using Entities.DbModels;
using Microsoft.Extensions.Logging;
using Services.NhlData;

namespace DataGetter.BusinessLogic;

/// <summary>
/// Saves NHL.com articles (league and team sites) with their tags, and a new version whenever an article's text
/// changes. Some articles are rewritten in place (daily props, trade and signing trackers), so those are fetched
/// every run even after they drop out of the newest-articles list.
/// </summary>
public partial class NhlArticleManager
{
    public const int LATEST_LIMIT = 100;
    // Articles NHL.com rewrites in place; the lineup projections article is saved separately (LineupArticle)
    public static readonly string[] RollingSlugs =
    [
        NhlArticleTextParser.PROPS_SLUG,
        "nhl-emergency-backup-goalies-list",
        "2026-27-nhl-trades",
        "free-agency-signings-nhl-2026-27",
    ];
    private readonly ISnapshotRepository _snapshotRepo;
    private readonly INhlContentGetter _contentGetter;
    private readonly ILogger<NhlArticleManager> _logger;

    public NhlArticleManager(ISnapshotRepository snapshotRepository, INhlContentGetter contentGetter, ILoggerFactory loggerFactory)
    {
        _snapshotRepo = snapshotRepository;
        _contentGetter = contentGetter;
        _logger = loggerFactory.CreateLogger<NhlArticleManager>();
    }

    /// <summary>
    /// Saves the newest articles that are new or were updated since they were last saved. Returns the failures
    /// (article url and exception) so one bad article doesn't stop the rest.
    /// </summary>
    public async Task<List<(string Url, Exception Error)>> SaveLatestArticles(DateTime nowUtc)
    {
        using var doc = JsonDocument.Parse(await _contentGetter.GetLatestStories(LATEST_LIMIT));
        var items = doc.RootElement.GetProperty("items").EnumerateArray()
            .Select(i => (EntityId: i.GetProperty("_entityId").GetString()!, Url: i.GetProperty("selfUrl").GetString()!,
                LastUpdated: ReadDate(i, "lastUpdatedDate")))
            .ToList();

        // Skip articles whose newest lastUpdatedDate is already saved
        var saved = (await _snapshotRepo.GetArticleVersions(items.Select(i => i.EntityId)))
            .Select(a => (a.EntityId, a.LastUpdated)).ToHashSet();
        var toFetch = items.Where(i => !saved.Contains((i.EntityId, i.LastUpdated))).ToList();

        var failures = new List<(string, Exception)>();
        var newVersions = 0;
        foreach (var item in toFetch)
        {
            try
            {
                if (await SaveArticle(await _contentGetter.GetStoryByUrl(item.Url), nowUtc, item.EntityId))
                    newVersions++;
            }
            catch (Exception ex)
            {
                failures.Add((item.Url, ex));
            }
        }
        _logger.LogInformation("Latest articles: {Listed} listed, {Fetched} fetched, {Saved} new versions, {Failed} failed",
            items.Count, toFetch.Count, newVersions, failures.Count);
        return failures;
    }

    /// <summary>Fetches the articles NHL.com rewrites in place and saves any new text.</summary>
    public async Task<List<(string Url, Exception Error)>> SaveRollingArticles(DateTime nowUtc)
    {
        var failures = new List<(string, Exception)>();
        foreach (var slug in RollingSlugs)
        {
            try
            {
                var saved = await SaveArticle(await _contentGetter.GetStoryBySlug(slug), nowUtc, null);
                _logger.LogInformation("Rolling article {Slug}: {Result}", slug, saved ? "saved new version" : "unchanged");
            }
            catch (Exception ex)
            {
                failures.Add((slug, ex));
            }
        }
        return failures;
    }

    /// <summary>Saves the article if its text is new and adds any new tags. Returns whether a version was added.</summary>
    private async Task<bool> SaveArticle(string json, DateTime nowUtc, string? expectedEntityId)
    {
        var (article, tags) = ParseArticle(json, nowUtc.AddTicks(-(nowUtc.Ticks % TimeSpan.TicksPerSecond)));
        if (expectedEntityId != null && article.EntityId != expectedEntityId)
            throw new Exception($"Article {article.Slug} came back as {article.EntityId}, expected {expectedEntityId}");

        var versions = await _snapshotRepo.GetArticleVersions([article.EntityId]);
        var known = versions.FirstOrDefault(v => v.ContentHash == article.ContentHash);
        var added = false;
        if (known == null)
        {
            var mentions = NhlArticleTextParser.ParseMentions(article.EntityId, article.ContentHash, article.Body);
            var picks = article.Slug == NhlArticleTextParser.PROPS_SLUG
                ? NhlArticleTextParser.ParsePropsPicks(article.EntityId, article.ContentHash, article.Body, article.ContentDate)
                : [];
            await _snapshotRepo.AddArticle(article, mentions, picks);
            added = true;
        }
        else if (known.LastUpdated != article.LastUpdated)
        {
            // Same text, newer save: remember it so the article isn't fetched again until it changes
            await _snapshotRepo.SetArticleLastUpdated(article.EntityId, article.ContentHash, article.LastUpdated);
        }

        var savedTags = (await _snapshotRepo.GetArticleTags(article.EntityId)).Select(t => t.TagSlug).ToHashSet();
        var newTags = tags.Where(t => !savedTags.Contains(t.TagSlug)).ToList();
        if (newTags.Count > 0)
            await _snapshotRepo.AddArticleTags(newTags);
        return added;
    }

    /// <summary>
    /// Reads an article and its tags. The body (and hash) is the markdown parts joined with "\n"; photos, videos and
    /// embeds are left out, so a save that only changes those or metadata isn't a new version.
    /// </summary>
    public static (DbNhlArticle Article, List<DbNhlArticleTag> Tags) ParseArticle(string json, DateTime firstSeenUtc)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var entityId = root.GetProperty("_entityId").GetString() ?? throw new Exception("Article has no _entityId");
        var body = root.TryGetProperty("parts", out var parts)
            ? string.Join("\n", parts.EnumerateArray()
                .Where(p => p.TryGetProperty("type", out var type) && type.GetString() == "markdown"
                    && p.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.String)
                .Select(p => p.GetProperty("content").GetString()))
            : string.Empty;

        var article = new DbNhlArticle
        {
            EntityId = entityId,
            ContentHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(body))),
            Slug = ReadString(root, "slug") ?? string.Empty,
            Title = ReadString(root, "title") ?? string.Empty,
            Headline = ReadString(root, "headline"),
            Summary = ReadString(root, "summary"),
            ContentDate = ReadDate(root, "contentDate"),
            LastUpdated = ReadDate(root, "lastUpdatedDate"),
            FirstSeenUTC = firstSeenUtc,
            Body = body,
        };

        var tags = new List<DbNhlArticleTag>();
        if (root.TryGetProperty("tags", out var tagArray))
        {
            foreach (var tag in tagArray.EnumerateArray())
            {
                var slug = ReadString(tag, "slug");
                if (string.IsNullOrEmpty(slug))
                    continue;
                var idMatch = NhlIdTagRegex().Match(slug);
                tags.Add(new DbNhlArticleTag
                {
                    EntityId = entityId,
                    TagSlug = slug,
                    Title = ReadString(tag, "title") ?? string.Empty,
                    SourceName = ReadString(tag, "externalSourceName"),
                    IdType = idMatch.Success ? idMatch.Groups[1].Value : null,
                    NhlId = idMatch.Success ? long.Parse(idMatch.Groups[2].Value) : null,
                });
            }
        }
        return (article, tags.DistinctBy(t => t.TagSlug).ToList());
    }

    [GeneratedRegex("^(team|player|game)id-(\\d+)$")]
    private static partial Regex NhlIdTagRegex();

    private static string? ReadString(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }

    private static DateTime? ReadDate(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            && DateTime.TryParse(value.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var date)
            ? date
            : null;
    }
}