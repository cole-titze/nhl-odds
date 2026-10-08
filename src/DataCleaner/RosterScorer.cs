using Entities.DbModels;
using Entities.Types;

namespace DataCleaner;

public record TeamRosterValues(
    double RosterOffenseValue,
    double RosterDefenseValue,
    double RosterGoalieValue,
    double RecentRosterOffenseValue,
    double RecentRosterDefenseValue,
    double RecentRosterGoalieValue
);

/// <summary>
/// Scores a team's roster for a game using only information available before puck drop, so a game's
/// values are the same whether it is upcoming (served to the predictor) or already played (training).
/// Skaters come from the team's most recent prior lineup; the goalie value is weighted by each goalie's
/// share of the team's recent starts, flipped toward the backup on the second night of a back-to-back.
/// </summary>
public class RosterScorer
{
    private const int RECENT_GAMES = 5;
    // Skaters with less total ice time than this get no rate (too little to measure)
    private const double MIN_SKATER_HOURS = 10 / 60.0;
    // Recent team starts used to estimate who will start in goal
    private const int STARTER_WINDOW = 10;
    // Games closer together than this count as a back-to-back
    private static readonly TimeSpan BACK_TO_BACK_GAP = TimeSpan.FromHours(30);
    // Share of back-to-back second nights started by the previous night's starter (2021-25: ~9%)
    private const double BACK_TO_BACK_REPEAT_SHARE = 0.1;
    // Save percentages are shrunk toward the previous season's league average with this many shots of prior,
    // so goalies and teams with few shots faced don't get extreme (or 0) values
    private const double SAVE_PCT_PRIOR_SHOTS = 1000;
    // Fallback league save percentage when the previous season isn't loaded
    private const double DEFAULT_LEAGUE_SAVE_PCT = 0.91;

    // gameId -> list of skater stats for that game
    private readonly Dictionary<int, List<DbGameSkaterStats>> _skaterStatsByGame = new();
    // playerId -> list of (gameId, skater stats) ordered by game date
    private readonly Dictionary<int, List<(int GameId, DateTime GameDate, DbGameSkaterStats Stats)>> _skaterHistory = new();
    // playerId -> list of (gameId, goalie stats) ordered by game date
    private readonly Dictionary<int, List<(int GameId, DateTime GameDate, DbGameGoalieStats Stats)>> _goalieHistory = new();
    // teamId -> games with a stored lineup, ordered by game date
    private readonly Dictionary<int, List<(int GameId, DateTime GameDate)>> _teamLineupGames = new();
    // teamId -> starting goalie of each played game, ordered by game date
    private readonly Dictionary<int, List<(DateTime GameDate, int GoalieId)>> _teamStarters = new();
    // teamId -> shots faced and saved by the team's goalies in each played game, ordered by game date
    private readonly Dictionary<int, List<(DateTime GameDate, int Season, int Saves, int Shots)>> _teamSaveTotals = new();
    // season start year -> league-wide save percentage
    private readonly Dictionary<int, double> _leagueSavePct = new();
    // teamId -> dates of every scheduled game, ordered
    private readonly Dictionary<int, List<DateTime>> _teamGameDates = new();
    // gameId -> game date lookup
    private readonly Dictionary<int, DateTime> _gameDates;

    public RosterScorer(
        IEnumerable<DbGameSkaterStats> allSkaterStats,
        IEnumerable<DbGameGoalieStats> allGoalieStats,
        IEnumerable<DbGameRaw> games)
    {
        var gameList = games.ToList();
        _gameDates = gameList.ToDictionary(g => g.Id, g => g.GameDateUTC);

        foreach (var game in gameList)
        {
            AddToList(_teamGameDates, game.HomeTeamId, game.GameDateUTC);
            AddToList(_teamGameDates, game.AwayTeamId, game.GameDateUTC);
        }
        foreach (var dates in _teamGameDates.Values)
            dates.Sort();

        var lineupKeys = new HashSet<(int TeamId, int GameId)>();
        foreach (var stat in allSkaterStats)
        {
            if (!_gameDates.TryGetValue(stat.GameId, out var gameDate))
                continue;

            AddToList(_skaterStatsByGame, stat.GameId, stat);
            AddToList(_skaterHistory, stat.PlayerId, (stat.GameId, gameDate, stat));
            if (lineupKeys.Add((stat.TeamId, stat.GameId)))
                AddToList(_teamLineupGames, stat.TeamId, (stat.GameId, gameDate));
        }

        foreach (var kvp in _skaterHistory)
            kvp.Value.Sort((a, b) => a.GameDate.CompareTo(b.GameDate));
        foreach (var kvp in _teamLineupGames)
            kvp.Value.Sort((a, b) => a.GameDate.CompareTo(b.GameDate));

        var goaliesByTeamGame = new Dictionary<(int TeamId, int GameId), List<DbGameGoalieStats>>();
        foreach (var stat in allGoalieStats)
        {
            if (!_gameDates.TryGetValue(stat.GameId, out var gameDate))
                continue;

            AddToList(goaliesByTeamGame, (stat.TeamId, stat.GameId), stat);
            AddToList(_goalieHistory, stat.PlayerId, (stat.GameId, gameDate, stat));
        }

        foreach (var kvp in _goalieHistory)
            kvp.Value.Sort((a, b) => a.GameDate.CompareTo(b.GameDate));

        var leagueTotals = new Dictionary<int, (int Saves, int Shots)>();
        // Starter of each team game, falling back to the goalie with the most TOI
        foreach (var ((teamId, gameId), goalies) in goaliesByTeamGame)
        {
            var starter = goalies.FirstOrDefault(g => g.IsStarter)
                ?? goalies.OrderByDescending(g => g.TimeOnIceSeconds).First();
            AddToList(_teamStarters, teamId, (_gameDates[gameId], starter.PlayerId));

            int season = GetSeason(gameId), saves = 0, shots = 0;
            foreach (var goalie in goalies)
            {
                var (goalieSaves, goalieShots) = GetSavesAndShots(goalie);
                saves += goalieSaves;
                shots += goalieShots;
            }
            AddToList(_teamSaveTotals, teamId, (_gameDates[gameId], season, saves, shots));
            var league = leagueTotals.GetValueOrDefault(season);
            leagueTotals[season] = (league.Saves + saves, league.Shots + shots);
        }
        foreach (var kvp in _teamStarters)
            kvp.Value.Sort((a, b) => a.GameDate.CompareTo(b.GameDate));
        foreach (var kvp in _teamSaveTotals)
            kvp.Value.Sort((a, b) => a.GameDate.CompareTo(b.GameDate));
        foreach (var (season, (saves, shots)) in leagueTotals)
        {
            if (shots > 0)
                _leagueSavePct[season] = (double)saves / shots;
        }
    }

    // The first 4 digits of a game ID are the season start year
    private static int GetSeason(int gameId) => gameId / 1_000_000;

    private double GetPriorSavePct(int gameId) =>
        _leagueSavePct.TryGetValue(GetSeason(gameId) - 1, out var pct) ? pct : DEFAULT_LEAGUE_SAVE_PCT;

    private static double ShrinkSavePct(int saves, int shots, double prior) =>
        (saves + SAVE_PCT_PRIOR_SHOTS * prior) / (shots + SAVE_PCT_PRIOR_SHOTS);

    /// <summary>
    /// Even-strength and power-play shots only. Shorthanded shots were stored as 0 for every game ingested before
    /// the mapper fix, so counting them only for new games would skew live values against training.
    /// </summary>
    private static (int Saves, int Shots) GetSavesAndShots(DbGameGoalieStats stats)
    {
        int saves = stats.EvenStrengthShotsSaved + stats.PowerPlayShotsSaved;
        int goalsAllowed = stats.EvenStrengthGoalsAllowed + stats.PowerPlayGoalsAllowed;
        return (saves, saves + goalsAllowed);
    }

    /// <summary>
    /// The team's save percentage this season and over its last RECENT_GAMES games, from its goalies' shots
    /// faced (so empty-net and shootout goals don't count), shrunk toward the previous season's league average.
    /// </summary>
    public (double SeasonSavePct, double RecentSavePct) GetTeamSavePct(int gameId, int teamId)
    {
        var prior = GetPriorSavePct(gameId);
        if (!_gameDates.TryGetValue(gameId, out var gameDate) || !_teamSaveTotals.TryGetValue(teamId, out var totals))
            return (prior, prior);

        var season = GetSeason(gameId);
        var priorGames = totals.Where(t => t.GameDate < gameDate).ToList();
        var seasonGames = priorGames.Where(t => t.Season == season).ToList();
        var recentGames = priorGames.TakeLast(RECENT_GAMES).ToList();

        return (
            ShrinkSavePct(seasonGames.Sum(t => t.Saves), seasonGames.Sum(t => t.Shots), prior),
            ShrinkSavePct(recentGames.Sum(t => t.Saves), recentGames.Sum(t => t.Shots), prior)
        );
    }

    private static void AddToList<TKey, TValue>(Dictionary<TKey, List<TValue>> map, TKey key, TValue value)
        where TKey : notnull
    {
        if (!map.TryGetValue(key, out var list))
        {
            list = new List<TValue>();
            map[key] = list;
        }
        list.Add(value);
    }

    public TeamRosterValues GetTeamRosterValues(int gameId, int teamId)
    {
        if (!_gameDates.TryGetValue(gameId, out var gameDate))
            return new TeamRosterValues(0, 0, 0, 0, 0, 0);

        var (seasonOffense, seasonDefense, recentOffense, recentDefense) = ComputeSkaterValues(teamId, gameDate);
        var (seasonGoalie, recentGoalie) = ComputeGoalieValues(teamId, gameDate, GetPriorSavePct(gameId));

        return new TeamRosterValues(
            seasonOffense, seasonDefense, seasonGoalie,
            recentOffense, recentDefense, recentGoalie
        );
    }

    /// <summary>
    /// The skaters the team dressed in its most recent game before the given date.
    /// </summary>
    private IEnumerable<DbGameSkaterStats> GetProjectedLineup(int teamId, DateTime gameDate)
    {
        if (!_teamLineupGames.TryGetValue(teamId, out var lineupGames))
            return [];

        var lastLineup = lineupGames.LastOrDefault(g => g.GameDate < gameDate);
        if (lastLineup == default)
            return [];

        return _skaterStatsByGame[lastLineup.GameId].Where(s => s.TeamId == teamId);
    }

    private (double SeasonOffense, double SeasonDefense, double RecentOffense, double RecentDefense) ComputeSkaterValues(
        int teamId, DateTime gameDate)
    {
        double seasonOffense = 0, seasonDefense = 0;
        double recentOffense = 0, recentDefense = 0;

        foreach (var skater in GetProjectedLineup(teamId, gameDate))
        {
            if (!_skaterHistory.TryGetValue(skater.PlayerId, out var history))
                continue;

            var priorGames = history.Where(h => h.GameDate < gameDate).ToList();
            if (priorGames.Count == 0)
                continue;

            // Season averages (all prior games)
            var (avgOff, avgDef) = AverageSkaterScores(priorGames, skater.Position);
            seasonOffense += avgOff;
            seasonDefense += avgDef;

            // Recent averages (last N prior games)
            var recentGames = priorGames.TakeLast(RECENT_GAMES).ToList();
            var (recentAvgOff, recentAvgDef) = AverageSkaterScores(recentGames, skater.Position);
            recentOffense += recentAvgOff;
            recentDefense += recentAvgDef;
        }

        return (seasonOffense, seasonDefense, recentOffense, recentDefense);
    }

    /// <summary>
    /// Per-60 offense and defense rates over the given games: weighted event totals divided by total ice time,
    /// so a game with a few seconds of TOI can't produce an extreme rate. Under MIN_SKATER_HOURS of ice time, 0.
    /// </summary>
    private static (double AvgOffense, double AvgDefense) AverageSkaterScores(
        List<(int GameId, DateTime GameDate, DbGameSkaterStats Stats)> games, POSITION position)
    {
        double totalOff = 0, totalDef = 0, totalHours = 0, totalFaceOffPct = 0;
        int count = 0;

        foreach (var (_, _, stats) in games)
        {
            if (stats.TimeOnIceSeconds == 0)
                continue;

            totalHours += stats.TimeOnIceSeconds / 3600.0;
            totalOff += 0.75 * stats.Goals + 0.63 * stats.Assists + 0.075 * stats.ShotsOnGoal
                + 0.15 * stats.PowerPlayGoals;
            totalDef += 0.10 * stats.BlockedShots + 0.08 * stats.Hits + 0.12 * stats.Takeaways
                + 0.05 * stats.PlusMinus - 0.075 * stats.PenaltyMinutes - 0.06 * stats.Giveaways;
            totalFaceOffPct += stats.FaceOffWinningPctg;
            count++;
        }

        if (count == 0 || totalHours < MIN_SKATER_HOURS)
            return (0, 0);

        double offense = totalOff / totalHours;
        if (position == POSITION.Center)
            offense += 0.10 * totalFaceOffPct / count;

        return (offense, totalDef / totalHours);
    }

    /// <summary>
    /// Probability that each goalie starts the team's game on the given date: their share of the team's
    /// last STARTER_WINDOW starts, excluding goalies whose latest appearance was for another team. On the
    /// second night of a back-to-back, the previous night's starter keeps only BACK_TO_BACK_REPEAT_SHARE.
    /// </summary>
    private Dictionary<int, double> GetStarterWeights(int teamId, DateTime gameDate)
    {
        if (!_teamStarters.TryGetValue(teamId, out var starters))
            return new Dictionary<int, double>();

        var recentStarts = starters.Where(s => s.GameDate < gameDate).TakeLast(STARTER_WINDOW).ToList();
        var startCounts = recentStarts
            .Where(s => LastTeamBefore(s.GoalieId, gameDate) == teamId)
            .GroupBy(s => s.GoalieId)
            .ToDictionary(g => g.Key, g => (double)g.Count());
        var totalStarts = startCounts.Values.Sum();
        if (totalStarts == 0)
            return new Dictionary<int, double>();

        var weights = startCounts.ToDictionary(kvp => kvp.Key, kvp => kvp.Value / totalStarts);

        var previousGameDate = GetPreviousGameDate(teamId, gameDate);
        var previousStart = recentStarts.LastOrDefault();
        var isBackToBack = previousGameDate != null && gameDate - previousGameDate.Value < BACK_TO_BACK_GAP;
        // Only flip when the previous night's starter is known (its stats have been stored)
        if (isBackToBack && previousStart.GameDate == previousGameDate && weights.ContainsKey(previousStart.GoalieId)
            && weights.Count > 1)
        {
            var otherTotal = 1 - weights[previousStart.GoalieId];
            foreach (var goalieId in weights.Keys.ToList())
            {
                weights[goalieId] = goalieId == previousStart.GoalieId
                    ? BACK_TO_BACK_REPEAT_SHARE
                    : weights[goalieId] / otherTotal * (1 - BACK_TO_BACK_REPEAT_SHARE);
            }
        }

        return weights;
    }

    private DateTime? GetPreviousGameDate(int teamId, DateTime gameDate)
    {
        if (!_teamGameDates.TryGetValue(teamId, out var dates))
            return null;

        var previous = dates.LastOrDefault(d => d < gameDate);
        return previous == default ? null : previous;
    }

    private int? LastTeamBefore(int goalieId, DateTime gameDate)
    {
        if (!_goalieHistory.TryGetValue(goalieId, out var history))
            return null;

        var last = history.LastOrDefault(h => h.GameDate < gameDate);
        return last == default ? null : last.Stats.TeamId;
    }

    private (double SeasonGoalie, double RecentGoalie) ComputeGoalieValues(int teamId, DateTime gameDate, double prior)
    {
        var weights = GetStarterWeights(teamId, gameDate);
        if (weights.Count == 0)
            return (prior, prior);

        double seasonGoalie = 0, recentGoalie = 0;
        foreach (var (goalieId, weight) in weights)
        {
            var priorGames = _goalieHistory[goalieId].Where(h => h.GameDate < gameDate).ToList();
            seasonGoalie += weight * GoalieSavePct(priorGames, prior);
            recentGoalie += weight * GoalieSavePct(priorGames.TakeLast(RECENT_GAMES).ToList(), prior);
        }

        return (seasonGoalie, recentGoalie);
    }

    private static double GoalieSavePct(List<(int GameId, DateTime GameDate, DbGameGoalieStats Stats)> games, double prior)
    {
        int totalSaves = 0, totalShots = 0;

        foreach (var (_, _, stats) in games)
        {
            var (saves, shots) = GetSavesAndShots(stats);
            totalSaves += saves;
            totalShots += shots;
        }

        return ShrinkSavePct(totalSaves, totalShots, prior);
    }
}
