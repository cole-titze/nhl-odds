using System.Globalization;
using System.Text;
using Entities.Types;
using Entities.ViewModels;

namespace WebApi.Calendar;

/// <summary>
/// A team's season as an iCalendar feed (RFC 5545) for calendar apps to subscribe to: one event
/// per game with the model's win probability (once there is one), the final score once played,
/// and a link to the game's page. Each event's UID is the game id, so a refresh updates events
/// in place instead of adding copies.
/// </summary>
public static class TeamCalendarBuilder
{
    // NHL games take about two and a half hours
    public static readonly TimeSpan GameLength = TimeSpan.FromMinutes(150);

    // Asks calendar apps to refresh every few hours (most pick their own interval anyway)
    private const string RefreshInterval = "PT3H";

    public static string Build(TeamVM team, int seasonStartYear, string siteUrl, DateTime nowUtc)
    {
        siteUrl = siteUrl.TrimEnd('/');
        var teamName = $"{team.LocationName} {team.TeamName}".Trim();
        var season = $"{seasonStartYear}-{(seasonStartYear + 1) % 100:D2}";
        var lines = new List<string>
        {
            "BEGIN:VCALENDAR",
            "VERSION:2.0",
            "PRODID:-//NHL Odds//Team calendar//EN",
            "CALSCALE:GREGORIAN",
            "METHOD:PUBLISH",
            Property("X-WR-CALNAME", $"{teamName} · NHL Odds"),
            Property("X-WR-CALDESC", $"{teamName} {season} games with NHL Odds model win probabilities. {siteUrl}/team/{team.Id}"),
            $"REFRESH-INTERVAL;VALUE=DURATION:{RefreshInterval}",
            $"X-PUBLISHED-TTL:{RefreshInterval}",
        };

        var host = new Uri(siteUrl).Host;
        var stamp = FormatUtc(nowUtc);
        foreach (var game in team.GameOddsVM.OrderBy(g => g.GameDate).ThenBy(g => g.Id))
        {
            var start = DateTime.SpecifyKind(game.GameDate, DateTimeKind.Utc);
            var url = $"{siteUrl}/game/{game.Id}";
            lines.Add("BEGIN:VEVENT");
            lines.Add($"UID:game-{game.Id}@{host}");
            lines.Add($"DTSTAMP:{stamp}");
            lines.Add($"DTSTART:{FormatUtc(start)}");
            lines.Add($"DTEND:{FormatUtc(start + GameLength)}");
            lines.Add(Property("SUMMARY", Summary(game, team.Id)));
            if (game.HomeTeam is not null)
                lines.Add(Property("LOCATION", game.HomeTeam.LocationName));
            lines.Add(Property("DESCRIPTION", Description(game, url)));
            lines.Add(Property("URL", url, escape: false));
            // Busy-free: a game on the calendar shouldn't block the day
            lines.Add("TRANSP:TRANSPARENT");
            lines.Add("END:VEVENT");
        }
        lines.Add("END:VCALENDAR");

        var sb = new StringBuilder();
        foreach (var line in lines)
            Fold(sb, line);
        return sb.ToString();
    }

    private static string Name(MatchupTeamVM? team) => team?.TeamName is { Length: > 0 } name ? name : "TBD";

    /// <summary>"Bruins at Maple Leafs (Maple Leafs 58%)" before, "Bruins 4, Maple Leafs 3 (Final)" after.</summary>
    public static string Summary(GameOddsVM game, int teamId)
    {
        var away = Name(game.AwayTeam);
        var home = Name(game.HomeTeam);
        if (game.HasBeenPlayed)
            return $"{away} {game.AwayTeam?.Goals ?? 0}, {home} {game.HomeTeam?.Goals ?? 0} (Final)";

        var summary = $"{away} at {home}";
        var us = game.HomeTeam?.Id == teamId ? game.HomeTeam : game.AwayTeam?.Id == teamId ? game.AwayTeam : null;
        if (us?.ModelOdds is { } odds)
            summary += $" ({Name(us)} {Percent(odds)})";
        return summary;
    }

    public static string Description(GameOddsVM game, string url)
    {
        var parts = new List<string>();
        if (game.HomeTeam?.ModelOdds is { } home && game.AwayTeam?.ModelOdds is { } away)
        {
            var verb = game.HasBeenPlayed ? "gave" : "gives";
            parts.Add($"The NHL Odds model {verb} the {Name(game.AwayTeam)} {Percent(away)} and the {Name(game.HomeTeam)} {Percent(home)}.");
            if (game.HasBeenPlayed)
            {
                var calledIt = home >= 0.5 == (game.Winner == Winner.HOME);
                parts.Add(calledIt ? "It picked the winner." : "It picked the loser.");
            }
        }
        else if (!game.HasBeenPlayed)
        {
            parts.Add("Model odds are posted closer to the game.");
        }
        parts.Add($"Odds and analysis: {url}");
        return string.Join("\n", parts);
    }

    private static string Percent(double probability) =>
        Math.Round(probability * 100).ToString("0", CultureInfo.InvariantCulture) + "%";

    private static string FormatUtc(DateTime utc) =>
        utc.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);

    private static string Property(string name, string value, bool escape = true) =>
        $"{name}:{(escape ? Escape(value) : value)}";

    // RFC 5545 3.3.11: backslash, semicolon, comma and newline are escaped in TEXT values
    public static string Escape(string text) => text
        .Replace("\\", "\\\\")
        .Replace(";", "\\;")
        .Replace(",", "\\,")
        .Replace("\r\n", "\\n")
        .Replace("\n", "\\n");

    // RFC 5545 3.1: lines end in CRLF and are folded at 75 octets, continuing after a space.
    // Folds fall between UTF-8 characters, never inside one.
    private static void Fold(StringBuilder sb, string line)
    {
        const int maxOctets = 75;
        var octets = 0;
        var limit = maxOctets;
        foreach (var rune in line.EnumerateRunes())
        {
            var size = rune.Utf8SequenceLength;
            if (octets + size > limit)
            {
                sb.Append("\r\n ");
                octets = 0;
                // The leading space counts toward the continuation line's length
                limit = maxOctets - 1;
            }
            sb.Append(rune.ToString());
            octets += size;
        }
        sb.Append("\r\n");
    }
}
