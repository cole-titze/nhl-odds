using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace WebApi;

/// <summary>
/// The site's readable page paths, as the frontend builds them (frontend/src/utils/pagePaths.ts; keep the two
/// in step): the id, then the names, e.g. /game/2026020123-oilers-at-maple-leafs. The site redirects any other
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

    /// <summary>A game's page, from the teams' names; a team still TBD leaves the names out</summary>
    public static string Game(int id, string? awayName, string? homeName)
    {
        var slug = string.IsNullOrEmpty(awayName) || string.IsNullOrEmpty(homeName) ? "" : Slugify($"{awayName} at {homeName}");
        return slug.Length == 0 ? $"/game/{id}" : $"/game/{id}-{slug}";
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NotSlug();
}
