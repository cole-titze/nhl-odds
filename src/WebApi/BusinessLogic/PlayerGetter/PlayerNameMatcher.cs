using System.Globalization;
using System.Text;
using Entities.Models.Web;

namespace WebApi.BusinessLogic.PlayerGetter;

/// <summary>
/// Finds players by name, ignoring case and accents ("stutzle" finds Stützle): the whole name, a last name,
/// or the start of each word ("mcdav", "con mcd"). Active players come first, then better matches.
/// </summary>
public static class PlayerNameMatcher
{
    public static List<PlayerName> Match(IEnumerable<PlayerName> players, string query, IReadOnlySet<int> activeIds, int limit)
    {
        var q = Fold(query);
        if (q.Length < 2)
            return new List<PlayerName>();
        var queryWords = q.Split(' ');
        var compactQuery = q.Replace(" ", "");

        return players
            .Select(p => (Player: p, Score: Score(p, q, queryWords, compactQuery)))
            .Where(x => x.Score >= 0)
            .OrderByDescending(x => activeIds.Contains(x.Player.Id))
            .ThenBy(x => x.Score)
            .ThenBy(x => x.Player.LastName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Player.FirstName, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .Select(x => x.Player)
            .ToList();
    }

    // Lower is better; -1 is no match
    private static int Score(PlayerName player, string q, string[] queryWords, string compactQuery)
    {
        var first = Fold(player.FirstName);
        var last = Fold(player.LastName);
        var full = $"{first} {last}";
        if (full == q)
            return 0;
        if (last == q)
            return 1;
        var nameWords = full.Split(' ');
        if (queryWords.All(w => nameWords.Any(n => n.StartsWith(w, StringComparison.Ordinal))))
            return 2;
        if (full.Replace(" ", "").Contains(compactQuery, StringComparison.Ordinal))
            return 3;
        return -1;
    }

    /// <summary>Lowercase without accents or punctuation: "Ekman-Larsson" is "ekman larsson", "O'Reilly" is "oreilly".</summary>
    public static string Fold(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (var c in text.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;
            if (char.IsLetterOrDigit(c))
                builder.Append(char.ToLowerInvariant(c));
            else if (char.IsWhiteSpace(c) || c == '-')
                builder.Append(' ');
        }
        // Letters FormD doesn't split (ø, ł, æ...) to their plain forms
        var folded = builder.ToString()
            .Replace('ø', 'o').Replace('ł', 'l').Replace('đ', 'd').Replace("æ", "ae").Replace("ß", "ss");
        return string.Join(' ', folded.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
