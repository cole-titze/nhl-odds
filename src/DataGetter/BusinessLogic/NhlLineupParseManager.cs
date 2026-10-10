using System.Globalization;
using System.Text;
using System.Text.Json;
using DatabaseAccess.LineupArticleRepository;
using Entities.DbModels;
using Microsoft.Extensions.Logging;
using Services.NhlData;

namespace DataGetter.BusinessLogic;

/// <summary>
/// Turns saved lineup article versions into LineupGame/LineupPlayer rows: each new game section, its game and teams,
/// and each listed player matched to an NHL player id through the team's current NHL roster.
/// </summary>
public class NhlLineupParseManager
{
    private readonly ILineupArticleRepository _lineupArticleRepo;
    private readonly INhlContentGetter _contentGetter;
    private readonly ILogger<NhlLineupParseManager> _logger;
    private readonly Dictionary<int, List<RosterPlayer>> _rosters = [];
    // Every player in the Player table by normalized name, for names not on the team's current roster
    private Dictionary<string, List<int>>? _allPlayers;

    public record RosterPlayer(int Id, string Name, char Position);

    public NhlLineupParseManager(ILineupArticleRepository lineupArticleRepository, INhlContentGetter contentGetter, ILoggerFactory loggerFactory)
    {
        _lineupArticleRepo = lineupArticleRepository;
        _contentGetter = contentGetter;
        _logger = loggerFactory.CreateLogger<NhlLineupParseManager>();
    }

    /// <summary>
    /// Parses every saved version not parsed yet, oldest first. Returns the failures (article hash and exception). A
    /// version that can't be parsed is marked failed so it isn't retried every run; a failed request is retried next run.
    /// </summary>
    public async Task<List<(string ArticleHash, Exception Error)>> ParseNewVersions(DateTime nowUtc)
    {
        var failures = new List<(string, Exception)>();
        var versions = await _lineupArticleRepo.GetUnparsedVersions();
        if (versions.Count == 0)
            return failures;
        var teams = await _lineupArticleRepo.GetLatestSeasonTeams();
        _allPlayers ??= (await _lineupArticleRepo.GetPlayerNames())
            .GroupBy(p => NormalizeName($"{p.FirstName} {p.LastName}"))
            .ToDictionary(g => g.Key, g => g.Select(p => p.Id).ToList());

        foreach (var version in versions)
        {
            try
            {
                var sections = await ParseVersion(version, teams);
                _logger.LogInformation("Parsed lineup article version {Hash}: {Sections} games, {New} new",
                    version.ContentHash[..8], sections.Total, sections.Games.Count);
                await _lineupArticleRepo.AddParsed(sections.Games, sections.Players, new DbLineupArticleParse
                {
                    ArticleHash = version.ContentHash,
                    ParsedUTC = nowUtc,
                    Sections = sections.Total,
                });
            }
            catch (HttpRequestException ex)
            {
                failures.Add((version.ContentHash, ex));
            }
            catch (Exception ex)
            {
                failures.Add((version.ContentHash, ex));
                await _lineupArticleRepo.AddParseFailure(new DbLineupArticleParse
                {
                    ArticleHash = version.ContentHash,
                    ParsedUTC = nowUtc,
                    Error = ex.Message,
                });
            }
        }
        return failures;
    }

    private async Task<(int Total, List<DbLineupGame> Games, List<DbLineupPlayer> Players)> ParseVersion(
        DbLineupArticle version, List<DbSeasonTeam> teams)
    {
        var markdown = Markdown(version.RawJson);
        // Fetch the rosters of every team in the article first, so the parser can use their positions
        var headingTeams = LineupArticleParser.Parse(markdown)
            .SelectMany(s => new[] { s.AwayName, s.HomeName })
            .Distinct()
            .ToDictionary(name => name, name => TeamIdOf(name, teams));
        foreach (var teamId in headingTeams.Values.OfType<int>().Distinct())
            await LoadRoster(teamId, teams);

        var sections = LineupArticleParser.Parse(markdown, (teamName, name) =>
            headingTeams.GetValueOrDefault(teamName) is int teamId ? UniquePosition(_rosters[teamId], name) : null);

        var editionStart = (version.ContentDate ?? version.FirstSeenUTC).AddHours(-12);
        var games = new List<DbLineupGame>();
        var players = new List<DbLineupPlayer>();
        foreach (var section in sections.DistinctBy(s => s.SectionHash))
        {
            if (await _lineupArticleRepo.SectionExists(section.SectionHash))
                continue;
            var awayId = headingTeams[section.AwayName];
            var homeId = headingTeams[section.HomeName];
            games.Add(new DbLineupGame
            {
                SectionHash = section.SectionHash,
                ArticleHash = version.ContentHash,
                FirstSeenUTC = version.FirstSeenUTC,
                GameId = awayId != null && homeId != null
                    ? await _lineupArticleRepo.FindGameId(awayId.Value, homeId.Value, editionStart, editionStart.AddDays(4))
                    : null,
                AwayTeamId = awayId,
                HomeTeamId = homeId,
                Heading = section.Heading,
                StatusReport = section.StatusReport,
            });
            players.AddRange(section.Players
                .DistinctBy(p => (p.Side, p.Group, p.LineNumber, p.Slot))
                .Select(p =>
                {
                    var teamId = p.Side == "Away" ? awayId : homeId;
                    return new DbLineupPlayer
                    {
                        SectionHash = section.SectionHash,
                        Side = p.Side,
                        Group = p.Group,
                        LineNumber = p.LineNumber,
                        Slot = p.Slot,
                        TeamId = teamId,
                        Name = p.Name,
                        PlayerId = (teamId != null ? MatchPlayer(_rosters[teamId.Value], p.Name, p.Group) : null)
                            ?? MatchAnyPlayer(_allPlayers!, p.Name),
                        Note = p.Note,
                    };
                }));
        }
        return (sections.Count, games, players);
    }

    private async Task LoadRoster(int teamId, List<DbSeasonTeam> teams)
    {
        if (_rosters.ContainsKey(teamId))
            return;
        var abbreviation = teams.First(t => t.TeamId == teamId).Abbreviation;
        _rosters[teamId] = ParseRoster(await _contentGetter.GetTeamRoster(abbreviation));
    }

    public static List<RosterPlayer> ParseRoster(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var players = new List<RosterPlayer>();
        foreach (var (group, position) in new[] { ("forwards", 'F'), ("defensemen", 'D'), ("goalies", 'G') })
        {
            if (!doc.RootElement.TryGetProperty(group, out var list))
                continue;
            players.AddRange(list.EnumerateArray().Select(p => new RosterPlayer(
                p.GetProperty("id").GetInt32(),
                NormalizeName($"{p.GetProperty("firstName").GetProperty("default").GetString()} {p.GetProperty("lastName").GetProperty("default").GetString()}"),
                position)));
        }
        return players;
    }

    /// <summary>
    /// The player with this exact name (accents, case and punctuation ignored). In a lineup group (F/D/G), a player of
    /// that position wins; otherwise the name has to be unique on the roster. Null rather than a guess.
    /// </summary>
    public static int? MatchPlayer(List<RosterPlayer> roster, string name, string group)
    {
        var candidates = roster.Where(p => p.Name == NormalizeName(name)).ToList();
        if (group is "F" or "D" or "G")
        {
            var samePosition = candidates.Where(p => p.Position == group[0]).ToList();
            if (samePosition.Count == 1)
                return samePosition[0].Id;
        }
        return candidates.Count == 1 ? candidates[0].Id : null;
    }

    /// <summary>
    /// For names not on the team's current NHL roster (injured reserve, just traded or sent down): the one player in the
    /// Player table with this exact name, or null when there are none or several.
    /// </summary>
    public static int? MatchAnyPlayer(Dictionary<string, List<int>> playersByName, string name)
    {
        return playersByName.TryGetValue(NormalizeName(name), out var ids) && ids.Count == 1 ? ids[0] : null;
    }

    private static char? UniquePosition(List<RosterPlayer> roster, string name)
    {
        var positions = roster.Where(p => p.Name == NormalizeName(name)).Select(p => p.Position).Distinct().ToList();
        return positions.Count == 1 ? positions[0] : null;
    }

    /// <summary>"FLYERS" → Philadelphia Flyers' id, by the end of the team's full name.</summary>
    public static int? TeamIdOf(string headingName, List<DbSeasonTeam> teams)
    {
        var name = NormalizeName(headingName);
        var matches = teams.Where(t => NormalizeName(t.Name).EndsWith(name)).ToList();
        return name.Length > 0 && matches.Count == 1 ? matches[0].TeamId : null;
    }

    public static string NormalizeName(string name)
    {
        var text = new StringBuilder();
        foreach (var c in name.Normalize(NormalizationForm.FormD))
        {
            if (char.IsLetter(c) && CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                text.Append(char.ToLowerInvariant(c));
        }
        return text.ToString();
    }

    private static string Markdown(string rawJson)
    {
        using var doc = JsonDocument.Parse(rawJson);
        return string.Join("\n", doc.RootElement.GetProperty("parts").EnumerateArray()
            .Where(p => p.TryGetProperty("type", out var type) && type.GetString() == "markdown"
                && p.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.String)
            .Select(p => p.GetProperty("content").GetString()));
    }
}