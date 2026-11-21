using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace WebApi;

/// <summary>
/// The site's readable page paths, as the frontend builds them (frontend/src/utils/pagePaths.ts; keep the two
/// in step): the id, then the names, e.g. /game/2026020123-oilers-at-maple-leafs-2026-11-21. The site redirects any other
/// form to this one, so a mismatch only costs a redirect.
/// </summary>
public static partial class PagePaths
{
    // Letters that don't split into a plain letter and an accent
    private static readonly Dictionary<char, string> Letters = new()
    {
        ['ø'] = "o", ['æ'] = "ae", ['œ'] = "oe", ['ß'] = "ss", ['ł'] = "l", ['đ'] = "d", ['ð'] = "d", ['þ'] = "th",
    };

    /// <summary>"Tim Stützle" → "tim-stutzle", "Ryan O'Reilly" → "ryan-oreilly"</summary>
    public static string Slugify(string text)
    {
        var builder = new StringBuilder();
        foreach (var c in text.ToLowerInvariant().Normalize(NormalizationForm.FormKD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark || c is '\'' or '’' or '.')
                continue;
            if (Letters.TryGetValue(c, out var plain))
                builder.Append(plain);
            else
                builder.Append(c);
        }
        return NotSlug().Replace(builder.ToString(), "-").Trim('-');
    }

    private static readonly TimeZoneInfo Central = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");

    /// <summary>
    /// A game's page, from the teams' names and its UTC start time (the URL has its day in Central time, like the
    /// Games feed): /game/2026020123-oilers-at-maple-leafs-2026-11-21. A team still TBD leaves the names out.
    /// </summary>
    public static string Game(int id, string? awayName, string? homeName, DateTime gameDateUtc)
    {
        var teams = string.IsNullOrEmpty(awayName) || string.IsNullOrEmpty(homeName) ? "" : $"{awayName} at {homeName}";
        var day = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(gameDateUtc, DateTimeKind.Utc), Central)
            .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return $"/game/{id}-{Slugify($"{teams} {day}")}";
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NotSlug();
}
