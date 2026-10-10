using System.Net;
using System.Text.Json;
using Services.NhlData;

namespace DataGetter.BusinessLogic;

/// <summary>
/// Finds NHL.com articles that are started fresh each season (lineup projections, daily props, trade and signing
/// trackers), so the slugs don't have to be changed by hand every fall. A season's articles count from July 1, when
/// free agency and the new trade tracker start.
/// </summary>
public static class NhlSeasonArticles
{
    private const int SEARCH_LIMIT = 20;

    /// <summary>An article's raw JSON and whether it belongs to the current season (false: last season's).</summary>
    public record SeasonArticle(string Slug, string Json, bool CurrentSeason);

    /// <summary>"2026-27" from July 2026 through June 2027.</summary>
    public static string Season(DateTime nowUtc)
    {
        var startYear = nowUtc.Month >= 7 ? nowUtc.Year : nowUtc.Year - 1;
        return $"{startYear}-{(startYear + 1) % 100:D2}";
    }

    public static string PreviousSeason(string season)
    {
        var startYear = int.Parse(season[..4]) - 1;
        return $"{startYear}-{(startYear + 1) % 100:D2}";
    }

    /// <summary>
    /// October through June: games are on, so the current season's lineup and props articles should exist. Before
    /// that, still reading last season's is expected.
    /// </summary>
    public static bool SeasonUnderway(DateTime nowUtc) => nowUtc.Month is >= 10 or < 7;

    public static bool IsLineupSlug(string slug) => slug.StartsWith("nhl-lineup-projections-");

    /// <summary>
    /// The daily props article: "nhl-picks-props-daily-fantasy-hockey-projections-for-2026-27-season" (2025-26 was
    /// "fantasy-hockey-picks-props-projections-for-2025-26-season", so the name isn't stable between seasons).
    /// </summary>
    public static bool IsPropsSlug(string slug) => slug.Contains("picks-props") && slug.EndsWith("-season");

    public static Task<SeasonArticle?> FindLineupArticle(INhlContentGetter getter, DateTime nowUtc)
    {
        return Find(getter, nowUtc, "lineup", IsLineupSlug, season => $"nhl-lineup-projections-{season}-season");
    }

    public static Task<SeasonArticle?> FindPropsArticle(INhlContentGetter getter, DateTime nowUtc)
    {
        return Find(getter, nowUtc, "fantasy", IsPropsSlug,
            season => $"nhl-picks-props-daily-fantasy-hockey-projections-for-{season}-season");
    }

    /// <summary>The trade and free-agent signing trackers for this season and last (both get edits around July 1).</summary>
    public static List<string> TrackerSlugs(DateTime nowUtc)
    {
        var season = Season(nowUtc);
        return [.. new[] { PreviousSeason(season), season }
            .SelectMany(s => new[] { $"{s}-nhl-trades", $"free-agency-signings-nhl-{s}" })];
    }

    /// <summary>
    /// The current season's article, else last season's, else null. Each season is looked up by its tag (newest
    /// article tagged with the season and <paramref name="tag"/> whose slug matches), then by the slug this season used.
    /// </summary>
    private static async Task<SeasonArticle?> Find(INhlContentGetter getter, DateTime nowUtc, string tag,
        Func<string, bool> isMatch, Func<string, string> slugFor)
    {
        var season = Season(nowUtc);
        foreach (var (s, current) in new[] { (season, true), (PreviousSeason(season), false) })
        {
            var slug = await SearchByTags(getter, [s, tag], slug => isMatch(slug) && slug.Contains(s));
            var json = slug != null ? await getter.GetStoryBySlug(slug) : await GetIfExists(getter, slug = slugFor(s));
            if (json != null)
                return new SeasonArticle(slug, json, current);
        }
        return null;
    }

    private static async Task<string?> SearchByTags(INhlContentGetter getter, string[] tags, Func<string, bool> isMatch)
    {
        using var doc = JsonDocument.Parse(await getter.GetStoriesByTags(tags, SEARCH_LIMIT));
        return doc.RootElement.GetProperty("items").EnumerateArray()
            .Select(i => i.GetProperty("slug").GetString())
            .FirstOrDefault(s => s != null && isMatch(s));
    }

    /// <summary>The article, or null when there's no article with this slug (other errors throw).</summary>
    public static async Task<string?> GetIfExists(INhlContentGetter getter, string slug)
    {
        try
        {
            return await getter.GetStoryBySlug(slug);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}