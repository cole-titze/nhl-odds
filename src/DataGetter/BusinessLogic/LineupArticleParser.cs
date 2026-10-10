using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace DataGetter.BusinessLogic;

/// <summary>
/// Reads NHL.com's lineup projections article: one "## AWAY (record) at HOME (record)" section per game, each with a
/// "**{Team} projected lineup**" block per team (forward lines "A -- B -- C", defense pairs "A -- B", goalies one per
/// line, then Scratched/Injured/Suspended lists) and a "Status report" paragraph.
/// </summary>
public static partial class LineupArticleParser
{
    public record ParsedPlayer(string Side, string Group, int LineNumber, int Slot, string Name, string? Note);

    public record ParsedSection(string SectionHash, string Heading, string AwayName, string HomeName, string? StatusReport,
        List<ParsedPlayer> Players);

    private static readonly string[] ListGroups = ["Scratched", "Injured", "Suspended"];

    /// <param name="markdown">The article's markdown parts joined with "\n"</param>
    /// <param name="positionOf">The roster position ('F', 'D' or 'G') of a name on a team (by its heading name), or null if
    /// unknown. Settles lines the name count gets wrong: a 2-name fourth line, a 7th defenseman listed alone.</param>
    public static List<ParsedSection> Parse(string markdown, Func<string, string, char?>? positionOf = null)
    {
        var sections = new List<ParsedSection>();
        var lines = markdown.Replace("\r", string.Empty).Split('\n');
        var start = -1;
        for (var i = 0; i <= lines.Length; i++)
        {
            // Other "## " lines (the start time, fantasy links) belong to the section they're in
            if (i < lines.Length && !(lines[i].StartsWith("## ") && HeadingRegex().IsMatch(Clean(lines[i].TrimStart('#')))))
                continue;
            if (start >= 0)
            {
                var section = ParseSection(lines[start..i], positionOf ?? ((_, _) => null));
                if (section != null)
                    sections.Add(section);
            }
            start = i;
        }
        return sections;
    }

    private static ParsedSection? ParseSection(string[] lines, Func<string, string, char?> positionOf)
    {
        var heading = Clean(lines[0].TrimStart('#'));
        var match = HeadingRegex().Match(heading);
        if (!match.Success)
            return null;
        var awayName = match.Groups["away"].Value.Trim();
        var homeName = match.Groups["home"].Value.Trim();

        var players = new List<ParsedPlayer>();
        var statusReport = new List<string>();
        string? side = null;
        var inStatusReport = false;
        var counts = new Dictionary<string, int>();
        foreach (var raw in lines.Skip(1))
        {
            var line = Clean(raw);
            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            var team = TeamHeaderRegex().Match(line);
            if (team.Success)
            {
                side = SideOf(team.Groups[1].Value, awayName, homeName);
                inStatusReport = false;
                counts.Clear();
                continue;
            }
            if (line.Equals("Status report", StringComparison.OrdinalIgnoreCase))
            {
                inStatusReport = true;
                side = null;
                continue;
            }
            if (inStatusReport)
            {
                statusReport.Add(raw.Trim());
                continue;
            }
            if (side == null)
                continue;

            var list = ListRegex().Match(line);
            if (list.Success)
            {
                var group = ListGroups.First(g => list.Groups[1].Value.StartsWith(g, StringComparison.OrdinalIgnoreCase));
                foreach (var (name, note) in SplitList(list.Groups[2].Value))
                    players.Add(new ParsedPlayer(side, group, Next(counts, group), 1, name, note));
                continue;
            }

            var names = LineSeparatorRegex().Split(line).Select(n => n.Trim()).Where(n => n.Length > 0).ToList();
            if (names.Count is < 1 or > 3 || !names.All(n => NameRegex().IsMatch(n)))
                continue;
            var lineGroup = GroupOf(names, side == "Away" ? awayName : homeName, counts, positionOf);
            var lineNumber = Next(counts, lineGroup);
            players.AddRange(names.Select((name, slot) => new ParsedPlayer(side, lineGroup, lineNumber, slot + 1, name, null)));
        }

        var text = string.Join("\n", lines);
        return new ParsedSection(
            Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text))),
            heading, awayName, homeName,
            statusReport.Count > 0 ? string.Join("\n", statusReport) : null,
            players);
    }

    /// <summary>
    /// Forward lines come first, then defense pairs, then goalies one per line. Three names is a forward line. Two names
    /// is a forward line until four of them are listed or a pair of defensemen shows up, then a defense pair. One name
    /// is a goalie. Where every name's roster position agrees, that wins instead.
    /// </summary>
    private static string GroupOf(List<string> names, string teamName, Dictionary<string, int> counts, Func<string, string, char?> positionOf)
    {
        var positions = names.Select(n => positionOf(teamName, n)).ToList();
        if (positions.All(p => p != null) && positions.Distinct().Count() == 1)
            return positions[0].ToString()!;
        return names.Count switch
        {
            3 => "F",
            2 when counts.GetValueOrDefault("F") < 4 && counts.GetValueOrDefault("D") == 0 && !positions.All(p => p == 'D') => "F",
            2 => "D",
            _ => "G",
        };
    }

    /// <summary>Splits "A (upper body), B; C" into names and notes, keeping commas inside parentheses.</summary>
    private static IEnumerable<(string Name, string? Note)> SplitList(string text)
    {
        var depth = 0;
        var current = new StringBuilder();
        var items = new List<string>();
        foreach (var c in text)
        {
            depth += c == '(' ? 1 : c == ')' ? -1 : 0;
            if ((c == ',' || c == ';') && depth == 0)
            {
                items.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        items.Add(current.ToString());

        foreach (var item in items.Select(i => i.Trim().TrimEnd('.')).Where(i => i.Length > 0))
        {
            if (item.Equals("None", StringComparison.OrdinalIgnoreCase))
                continue;
            var note = NoteRegex().Match(item);
            yield return note.Success
                ? (note.Groups[1].Value.Trim(), note.Groups[2].Value.Trim())
                : (item, null);
        }
    }

    private static string? SideOf(string teamName, string awayName, string homeName)
    {
        var name = teamName.Trim().ToUpperInvariant();
        if (name == awayName.ToUpperInvariant())
            return "Away";
        if (name == homeName.ToUpperInvariant())
            return "Home";
        return null;
    }

    private static int Next(Dictionary<string, int> counts, string group)
    {
        counts[group] = counts.GetValueOrDefault(group) + 1;
        return counts[group];
    }

    // Drops markdown emphasis, links ("[text](url)" → "text") and non-breaking spaces
    private static string Clean(string line)
    {
        var text = LinkRegex().Replace(line, "$1").Replace("*", string.Empty).Replace(' ', ' ');
        return WhitespaceRegex().Replace(text, " ").Trim();
    }

    [GeneratedRegex("^(?<away>[^()]+?)\\s*(\\([^)]*\\))?\\s+at\\s+(?<home>[^()]+?)\\s*(\\([^)]*\\))?$")]
    private static partial Regex HeadingRegex();

    [GeneratedRegex("^(.+?) projected (?:lineup|lines):?$", RegexOptions.IgnoreCase)]
    private static partial Regex TeamHeaderRegex();

    [GeneratedRegex("^(Scratched|Injured|Suspended)[^:]*:\\s*(.*)$", RegexOptions.IgnoreCase)]
    private static partial Regex ListRegex();

    [GeneratedRegex("\\s*(?:-{2,}|–|—)\\s*")]
    private static partial Regex LineSeparatorRegex();

    // A goalie line: two to four words of letters (no colon or sentence punctuation)
    [GeneratedRegex("^[\\p{L}'’.\\-]+(?: [\\p{L}'’.\\-]+){1,3}$")]
    private static partial Regex NameRegex();

    [GeneratedRegex("^(.+?)\\s*\\((.+)\\)$")]
    private static partial Regex NoteRegex();

    [GeneratedRegex("\\[([^\\]]*)\\]\\([^)]*\\)")]
    private static partial Regex LinkRegex();

    [GeneratedRegex("\\s+")]
    private static partial Regex WhitespaceRegex();
}