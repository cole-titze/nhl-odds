using Entities.Models.Web;
using Entities.ViewModels;
using WebApi.Mappers;

namespace WebApi.BusinessLogic.StatLeaders;

/// <summary>One stat the leaders can be ranked by.</summary>
/// <param name="Key">The API and URL name, e.g. "points"</param>
/// <param name="IsRate">Per game or a percentage: only players with enough games count (see <see cref="StatLeaders.MinGames"/>)</param>
/// <param name="Value">The stat from a player's totals, or null when it doesn't apply (no shots faced)</param>
public record LeaderStat(string Key, string Label, bool Goalie, bool IsRate, bool LowerIsBetter,
    Func<PlayerSeasonTotals, double?> Value);

/// <summary>
/// League leaders by stat from regular-season totals (#182). Wins aren't offered: the box scores don't record
/// which goalie got the decision.
/// </summary>
public static class StatLeaders
{
    public const int MaxLeaders = 50;

    // A rate stat counts players with at least this share of the most games anyone in the group has played,
    // so a goalie with one great start doesn't lead save percentage
    private const double MinGamesShare = 0.25;

    public static readonly IReadOnlyList<LeaderStat> Stats =
    [
        new("points", "Points", false, false, false, t => t.Goals + t.Assists),
        new("goals", "Goals", false, false, false, t => t.Goals),
        new("assists", "Assists", false, false, false, t => t.Assists),
        new("shots", "Shots on goal", false, false, false, t => t.Shots),
        new("plusMinus", "Plus/minus", false, false, false, t => t.PlusMinus),
        new("powerPlayGoals", "Power play goals", false, false, false, t => t.PowerPlayGoals),
        new("penaltyMinutes", "Penalty minutes", false, false, false, t => t.PenaltyMinutes),
        new("hits", "Hits", false, false, false, t => t.Hits),
        new("blockedShots", "Blocked shots", false, false, false, t => t.BlockedShots),
        new("timeOnIcePerGame", "Time on ice per game", false, true, false,
            t => t.GamesPlayed > 0 ? (double)t.TimeOnIceSeconds / t.GamesPlayed : null),
        new("savePercentage", "Save percentage", true, true, false,
            t => t.Saves + t.GoalsAgainst > 0 ? (double)t.Saves / (t.Saves + t.GoalsAgainst) : null),
        new("goalsAgainstAverage", "Goals against average", true, true, true,
            t => t.TimeOnIceSeconds > 0 ? t.GoalsAgainst * 3600.0 / t.TimeOnIceSeconds : null),
        new("saves", "Saves", true, false, false, t => t.Saves),
        new("shutouts", "Shutouts", true, false, false, t => t.Shutouts),
        new("gamesStarted", "Games started", true, false, false, t => t.GamesStarted),
    ];

    public static LeaderStat? Find(string? key) =>
        Stats.FirstOrDefault(s => string.Equals(s.Key, key, StringComparison.OrdinalIgnoreCase));

    /// <summary>The fewest games a player needs to count for a rate stat (0 for totals).</summary>
    public static int MinGames(LeaderStat stat, IEnumerable<PlayerSeasonTotals> group)
    {
        if (!stat.IsRate)
            return 0;
        var most = group.Select(t => t.GamesPlayed).DefaultIfEmpty(0).Max();
        return Math.Max(1, (int)Math.Ceiling(most * MinGamesShare));
    }

    public static StatLeadersVM Rank(IEnumerable<PlayerSeasonTotals> totals, LeaderStat stat, int seasonStartYear,
        int limit = MaxLeaders)
    {
        var group = totals.Where(t => t.IsGoalie == stat.Goalie && t.GamesPlayed > 0).ToList();
        var minGames = MinGames(stat, group);
        var ranked = group
            .Where(t => t.GamesPlayed >= minGames)
            .Select(t => (Totals: t, Value: stat.Value(t)))
            .Where(x => x.Value.HasValue)
            .Select(x => (x.Totals, Value: x.Value!.Value))
            .OrderBy(x => stat.LowerIsBetter ? x.Value : -x.Value)
            // Ties: fewer games first (did it quicker), then by name
            .ThenBy(x => x.Totals.GamesPlayed)
            .ThenBy(x => x.Totals.LastName, StringComparer.Ordinal)
            .ThenBy(x => x.Totals.FirstName, StringComparer.Ordinal)
            .Take(Math.Clamp(limit, 1, MaxLeaders))
            .ToList();

        var leaders = new List<StatLeaderVM>(ranked.Count);
        for (var i = 0; i < ranked.Count; i++)
        {
            var (t, value) = ranked[i];
            // Shared ranks: the same value as the row above keeps its rank (1, 2, 2, 4)
            var rank = i > 0 && SameValue(ranked[i - 1].Value, value) ? leaders[i - 1].Rank : i + 1;
            leaders.Add(new StatLeaderVM
            {
                Rank = rank,
                PlayerId = t.PlayerId,
                FirstName = t.FirstName,
                LastName = t.LastName,
                HeadShot = t.HeadShot,
                Position = PlayerToPlayerVmMapper.PositionCode(t.Position),
                TeamId = t.TeamId,
                TeamAbbreviation = t.TeamAbbreviation,
                TeamName = t.TeamName,
                TeamLogoUri = t.TeamLogoUri,
                GamesPlayed = t.GamesPlayed,
                Value = value,
            });
        }

        return new StatLeadersVM
        {
            SeasonStartYear = seasonStartYear,
            Stat = stat.Key,
            Label = stat.Label,
            Group = stat.Goalie ? "goalie" : "skater",
            LowerIsBetter = stat.LowerIsBetter,
            MinGamesPlayed = minGames,
            Leaders = leaders,
        };
    }

    // Rates are compared as shown (save percentage to 3 places, GAA to 2, time on ice to the second)
    private static bool SameValue(double a, double b) => Math.Abs(a - b) < 1e-9;
}
