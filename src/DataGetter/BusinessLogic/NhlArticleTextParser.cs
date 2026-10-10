using System.Globalization;
using System.Text.RegularExpressions;
using Entities.DbModels;

namespace DataGetter.BusinessLogic;

/// <summary>Pulls structured rows out of NHL.com article text: linked players and the daily props picks.</summary>
public static partial class NhlArticleTextParser
{
    public const string PROPS_SLUG = "nhl-picks-props-daily-fantasy-hockey-projections-for-2026-27-season";

    /// <summary>Every linked entity (&lt;forge-entity slug="name-8482809" code="player"&gt;), once each with its count.</summary>
    public static List<DbNhlArticleMention> ParseMentions(string entityId, string contentHash, string body)
    {
        return EntityLinkRegex().Matches(body)
            .Select(m => (Title: WebUtility(m.Groups["title"].Value), Code: m.Groups["code"].Value, Id: long.Parse(m.Groups["id"].Value)))
            .GroupBy(m => (m.Code, m.Id))
            .Select(g => new DbNhlArticleMention
            {
                EntityId = entityId,
                ContentHash = contentHash,
                Code = g.Key.Code,
                NhlId = g.Key.Id,
                Title = g.First().Title,
                Count = g.Count(),
            })
            .ToList();
    }

    /// <summary>
    /// The picks in the props article: the "Player to watch for ..." picks under NHL PROPS, the "Others to watch for ..."
    /// lists, and the Goal Chase and Fantasy Stars picks. Each pick is a linked player followed by "F, CAR (at CHI".
    /// </summary>
    public static List<DbNhlPropsPick> ParsePropsPicks(string entityId, string contentHash, string body, DateTime? articleDate)
    {
        var picks = new List<DbNhlPropsPick>();
        string? section = null;
        string? category = null;
        DateTime? pickDate = null;
        foreach (var rawLine in body.Split('\n'))
        {
            var line = rawLine.Replace("*", string.Empty).Trim();
            if (rawLine.TrimStart().StartsWith("## "))
            {
                var heading = line.TrimStart('#').Trim().ToUpperInvariant();
                section = heading.Contains("PROPS") ? "Props"
                    : heading.Contains("GOAL CHASE") ? "Goal Chase"
                    : heading.Contains("FANTASY STARS") ? "Fantasy Stars"
                    : null;
                category = section == "Props" ? null : section;
                if (section == "Props")
                    pickDate = ParseHeadingDate(heading, articleDate) ?? pickDate;
                continue;
            }
            if (section == null)
                continue;

            var others = OthersRegex().Match(line);
            if (others.Success)
            {
                category = others.Groups[1].Value.Trim();
                continue;
            }
            var pick = PickRegex().Match(line);
            if (!pick.Success)
                continue;
            // "Player to watch for point, ...: <player>, F, CAR (at CHI" carries its own category
            var prefix = line[..pick.Index].Trim().TrimEnd(':').Trim();
            var pickCategory = prefix.StartsWith("Player to watch for", StringComparison.OrdinalIgnoreCase) ? prefix : category;
            if (pickCategory == null)
                continue;
            picks.Add(new DbNhlPropsPick
            {
                EntityId = entityId,
                ContentHash = contentHash,
                Category = pickCategory,
                PlayerId = int.Parse(pick.Groups["id"].Value),
                PickDate = pickDate,
                Position = pick.Groups["pos"].Value,
                TeamAbbreviation = pick.Groups["team"].Value,
                OpponentAbbreviation = pick.Groups["opp"].Value,
                IsHome = pick.Groups["where"].Value.StartsWith("vs"),
            });
        }
        return picks.DistinctBy(p => (p.Category, p.PlayerId)).ToList();
    }

    /// <summary>"NHL PROPS: SAT. OCT. 10" → Oct. 10 of the year that puts it closest to the article's date.</summary>
    private static DateTime? ParseHeadingDate(string heading, DateTime? articleDate)
    {
        var match = HeadingDateRegex().Match(heading);
        if (!match.Success)
            return null;
        var month = DateTime.ParseExact(match.Groups[1].Value[..3], "MMM", CultureInfo.InvariantCulture).Month;
        var day = int.Parse(match.Groups[2].Value);
        var reference = articleDate ?? DateTime.UtcNow;
        return new[] { reference.Year - 1, reference.Year, reference.Year + 1 }
            .Where(y => day <= DateTime.DaysInMonth(y, month))
            .Select(y => new DateTime(y, month, day))
            .OrderBy(d => Math.Abs((d - reference.Date).TotalDays))
            .First();
    }

    private static string WebUtility(string text) => System.Net.WebUtility.HtmlDecode(text);

    [GeneratedRegex("<forge-entity title=\"(?<title>[^\"]*)\" slug=\"[^\"]*?-(?<id>\\d+)\" code=\"(?<code>[^\"]+)\"")]
    private static partial Regex EntityLinkRegex();

    [GeneratedRegex("<forge-entity [^>]*slug=\"[^\"]*?-(?<id>\\d+)\" code=\"player\">[^<]*</forge-entity>,?\\s*(?<pos>[FDG]),\\s*(?<team>[A-Z]{3})\\s*\\((?<where>at|vs\\.?)\\s+(?<opp>[A-Z]{3})")]
    private static partial Regex PickRegex();

    [GeneratedRegex("^(Others to watch for [^:]+):?$", RegexOptions.IgnoreCase)]
    private static partial Regex OthersRegex();

    [GeneratedRegex("(JAN|FEB|MAR|APR|MAY|JUNE?|JULY?|AUG|SEPT?|OCT|NOV|DEC)[A-Z]*\\.?\\s+(\\d{1,2})\\b")]
    private static partial Regex HeadingDateRegex();
}