using System.Net;
using System.Text.RegularExpressions;
using Entities.DbModels;
using Entities.Types.Enums;

namespace Search;

public record TeamDocument(int TeamId, string Name, string CommonName, string PlaceName, List<string> FormerNames,
    string Abbreviation, string LogoUri, bool Active, int LastSeason);

public record PlayerDocument(int PlayerId, string FullName, string? Position, int? TeamId, string? TeamName,
    string? TeamAbbreviation, bool IsActive, string Headshot);

public record GameTeamDocument(int TeamId, string Name, string Abbreviation, int? Goals);

public record GameDocument(int GameId, string GameDate, DateTime StartTimeUtc, int Season, string GameType, bool Played,
    GameTeamDocument Home, GameTeamDocument Away);

/// <param name="EmbeddingKey">Which text and model <paramref name="Embedding"/> came from (see <see cref="ArticleEmbeddings"/>),
/// so the next rebuild can reuse an unchanged article's vector instead of asking the model again</param>
/// <param name="Embedding">The article as a vector, for semantic search; null when the embedding server is off or
/// didn't get to it this run (keyword search still finds it)</param>
public record ArticleDocument(string EntityId, string Title, string? Summary, string Body, DateTime? Date, string Url,
    string? EmbeddingKey = null, float[]? Embedding = null);

/// <summary>A player row, the position from their latest game roster and their current team (see
/// ISearchSourceRepository.GetPlayers), if any.</summary>
public record PlayerRow(DbPlayer Player, string? Position, int? CurrentTeamId);

/// <summary>Builds the search documents from database rows.</summary>
public static partial class SearchDocumentMapper
{
    private static readonly TimeZoneInfo Eastern = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    /// <summary>
    /// One document per team id, named as in its latest season; older names (e.g. before a rename) are kept as
    /// <see cref="TeamDocument.FormerNames"/>. A team is active if it played in the latest season.
    /// </summary>
    public static List<TeamDocument> MapTeams(IEnumerable<DbSeasonTeam> seasonTeams)
    {
        var rows = seasonTeams.ToList();
        if (rows.Count == 0)
            return [];
        var latestSeason = rows.Max(r => r.SeasonStartYear);
        return rows
            .GroupBy(r => r.TeamId)
            .Select(g =>
            {
                var latest = g.MaxBy(r => r.SeasonStartYear)!;
                var former = g.Select(r => r.Name).Where(n => n != latest.Name).Distinct().ToList();
                return new TeamDocument(latest.TeamId, latest.Name, latest.CommonName, latest.PlaceName, former,
                    latest.Abbreviation, latest.LogoUri, latest.SeasonStartYear == latestSeason, latest.SeasonStartYear);
            })
            .OrderBy(t => t.TeamId)
            .ToList();
    }

    /// <summary>Players with their current team's latest name; no current team, or an id no team has, is left empty.</summary>
    public static List<PlayerDocument> MapPlayers(IEnumerable<PlayerRow> players, IReadOnlyList<TeamDocument> teams)
    {
        var teamsById = teams.ToDictionary(t => t.TeamId);
        return players
            .Where(p => !string.IsNullOrWhiteSpace(p.Player.FirstName) || !string.IsNullOrWhiteSpace(p.Player.LastName))
            .Select(p =>
            {
                var team = p.CurrentTeamId is { } id ? teamsById.GetValueOrDefault(id) : null;
                return new PlayerDocument(p.Player.Id, $"{p.Player.FirstName} {p.Player.LastName}".Trim(),
                    string.IsNullOrEmpty(p.Position) ? null : p.Position, team?.TeamId, team?.Name, team?.Abbreviation,
                    p.Player.IsActive, p.Player.HeadShot);
            })
            .ToList();
    }

    /// <summary>Games between known teams, named as each team was named that season.</summary>
    public static List<GameDocument> MapGames(IEnumerable<DbGameRaw> games, IEnumerable<DbSeasonTeam> seasonTeams)
    {
        var names = seasonTeams.ToDictionary(t => (t.TeamId, t.SeasonStartYear));
        var latestNames = seasonTeams.GroupBy(t => t.TeamId).ToDictionary(g => g.Key, g => g.MaxBy(t => t.SeasonStartYear)!);

        GameTeamDocument? Side(int teamId, int season, int goals, bool played)
        {
            var team = names.GetValueOrDefault((teamId, season)) ?? latestNames.GetValueOrDefault(teamId);
            return team == null ? null : new GameTeamDocument(teamId, team.Name, team.Abbreviation, played ? goals : null);
        }

        var documents = new List<GameDocument>();
        foreach (var game in games)
        {
            var home = Side(game.HomeTeamId, game.SeasonStartYear, game.HomeGoals, game.HasBeenPlayed);
            var away = Side(game.AwayTeamId, game.SeasonStartYear, game.AwayGoals, game.HasBeenPlayed);
            if (home == null || away == null)
                continue;
            var startUtc = DateTime.SpecifyKind(game.GameDateUTC, DateTimeKind.Utc);
            var easternDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(startUtc, Eastern));
            documents.Add(new GameDocument(game.Id, easternDate.ToString("yyyy-MM-dd"), startUtc, game.SeasonStartYear,
                game.GameType.ToString(), game.HasBeenPlayed, home, away));
        }
        return documents;
    }

    /// <summary>
    /// The newest version of each article, as plain text, each story once: NHL.com reposts a story on team sites as
    /// its own article whose slug is the original's plus <c>-x</c> and four digits (a game recap often three times),
    /// so those count as one, the original's copy if there is one, else the newest.
    /// </summary>
    public static List<ArticleDocument> MapArticles(IEnumerable<DbNhlArticle> versions)
    {
        return versions
            .GroupBy(a => a.EntityId)
            .Select(g => g.MaxBy(a => a.FirstSeenUTC)!)
            .Where(a => !string.IsNullOrWhiteSpace(a.Slug))
            .GroupBy(a => RepostSuffixRegex().Replace(a.Slug, ""))
            .Select(g => g.FirstOrDefault(a => a.Slug == g.Key) ?? g.MaxBy(a => a.ContentDate ?? a.LastUpdated ?? a.FirstSeenUTC)!)
            .Select(a => new ArticleDocument(a.EntityId, string.IsNullOrWhiteSpace(a.Headline) ? a.Title : a.Headline,
                a.Summary, MarkdownToText(a.Body), a.ContentDate ?? a.LastUpdated ?? a.FirstSeenUTC, ArticleUrl(a.Slug)))
            .ToList();
    }

    public static string ArticleUrl(string slug) => $"https://www.nhl.com/news/{Uri.EscapeDataString(slug)}";

    /// <summary>
    /// Removes the markdown the NHL's articles use: links and images keep their text, emphasis and headings their
    /// words. Their inline HTML goes too (about half the articles link players with
    /// <c>&lt;forge-entity&gt;</c>, and some use <c>&lt;a&gt;</c>, <c>&lt;sup&gt;</c> or <c>&lt;s&gt;</c>), keeping the text
    /// inside, and entities like <c>&amp;amp;</c> become their characters, so snippets never show markup.
    /// </summary>
    public static string MarkdownToText(string markdown)
    {
        var text = ImageRegex().Replace(markdown, "");
        text = LinkRegex().Replace(text, "$1");
        text = HtmlTagRegex().Replace(text, "");
        text = WebUtility.HtmlDecode(text);
        text = HeadingRegex().Replace(text, "");
        text = EmphasisRegex().Replace(text, "");
        text = WhitespaceRegex().Replace(text, " ");
        return text.Trim();
    }

    // A team site's repost of a league story: the original's slug plus "-x" and four digits
    [GeneratedRegex(@"-x\d{4}$")]
    private static partial Regex RepostSuffixRegex();
    [GeneratedRegex(@"!\[[^\]]*\]\([^)]*\)")]
    private static partial Regex ImageRegex();
    [GeneratedRegex(@"\[([^\]]*)\]\([^)]*\)")]
    private static partial Regex LinkRegex();
    [GeneratedRegex(@"</?[a-zA-Z][a-zA-Z0-9-]*(\s[^<>]*)?/?>")]
    private static partial Regex HtmlTagRegex();
    [GeneratedRegex(@"(?m)^\s{0,3}(#{1,6}|>|[-*+]|\d+\.)\s+")]
    private static partial Regex HeadingRegex();
    [GeneratedRegex(@"\*\*|__|(?<!\w)[*_](?!\s)|(?<!\s)[*_](?!\w)|`")]
    private static partial Regex EmphasisRegex();
    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
