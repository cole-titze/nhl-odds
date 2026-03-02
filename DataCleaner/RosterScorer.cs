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

public class RosterScorer
{
    private const int RECENT_GAMES = 5;

    // gameId -> list of skater stats for that game
    private readonly Dictionary<int, List<DbGameSkaterStats>> _skaterStatsByGame;
    // playerId -> list of (gameId, skater stats) ordered by game date
    private readonly Dictionary<int, List<(int GameId, DateTime GameDate, DbGameSkaterStats Stats)>> _skaterHistory;
    // gameId -> list of goalie stats for that game
    private readonly Dictionary<int, List<DbGameGoalieStats>> _goalieStatsByGame;
    // playerId -> list of (gameId, goalie stats) ordered by game date
    private readonly Dictionary<int, List<(int GameId, DateTime GameDate, DbGameGoalieStats Stats)>> _goalieHistory;
    // gameId -> game date lookup
    private readonly Dictionary<int, DateTime> _gameDates;

    public RosterScorer(
        IEnumerable<DbGameSkaterStats> allSkaterStats,
        IEnumerable<DbGameGoalieStats> allGoalieStats,
        IEnumerable<DbGameRaw> games)
    {
        _gameDates = games.ToDictionary(g => g.Id, g => g.GameDateUTC);

        // Build skater indexes
        _skaterStatsByGame = new Dictionary<int, List<DbGameSkaterStats>>();
        _skaterHistory = new Dictionary<int, List<(int, DateTime, DbGameSkaterStats)>>();

        foreach (var stat in allSkaterStats)
        {
            if (!_gameDates.TryGetValue(stat.GameId, out var gameDate))
                continue;

            if (!_skaterStatsByGame.ContainsKey(stat.GameId))
                _skaterStatsByGame[stat.GameId] = new List<DbGameSkaterStats>();
            _skaterStatsByGame[stat.GameId].Add(stat);

            if (!_skaterHistory.ContainsKey(stat.PlayerId))
                _skaterHistory[stat.PlayerId] = new List<(int, DateTime, DbGameSkaterStats)>();
            _skaterHistory[stat.PlayerId].Add((stat.GameId, gameDate, stat));
        }

        // Sort each player's history by game date
        foreach (var kvp in _skaterHistory)
            kvp.Value.Sort((a, b) => a.GameDate.CompareTo(b.GameDate));

        // Build goalie indexes
        _goalieStatsByGame = new Dictionary<int, List<DbGameGoalieStats>>();
        _goalieHistory = new Dictionary<int, List<(int, DateTime, DbGameGoalieStats)>>();

        foreach (var stat in allGoalieStats)
        {
            if (!_gameDates.TryGetValue(stat.GameId, out var gameDate))
                continue;

            if (!_goalieStatsByGame.ContainsKey(stat.GameId))
                _goalieStatsByGame[stat.GameId] = new List<DbGameGoalieStats>();
            _goalieStatsByGame[stat.GameId].Add(stat);

            if (!_goalieHistory.ContainsKey(stat.PlayerId))
                _goalieHistory[stat.PlayerId] = new List<(int, DateTime, DbGameGoalieStats)>();
            _goalieHistory[stat.PlayerId].Add((stat.GameId, gameDate, stat));
        }

        foreach (var kvp in _goalieHistory)
            kvp.Value.Sort((a, b) => a.GameDate.CompareTo(b.GameDate));
    }

    public TeamRosterValues GetTeamRosterValues(int gameId, int teamId)
    {
        if (!_gameDates.TryGetValue(gameId, out var gameDate))
            return new TeamRosterValues(0, 0, 0, 0, 0, 0);

        var (seasonOffense, seasonDefense, recentOffense, recentDefense) = ComputeSkaterValues(gameId, teamId, gameDate);
        var (seasonGoalie, recentGoalie) = ComputeGoalieValues(gameId, teamId, gameDate);

        return new TeamRosterValues(
            seasonOffense, seasonDefense, seasonGoalie,
            recentOffense, recentDefense, recentGoalie
        );
    }

    private (double SeasonOffense, double SeasonDefense, double RecentOffense, double RecentDefense) ComputeSkaterValues(
        int gameId, int teamId, DateTime gameDate)
    {
        double seasonOffense = 0, seasonDefense = 0;
        double recentOffense = 0, recentDefense = 0;

        if (!_skaterStatsByGame.TryGetValue(gameId, out var gameSkaters))
            return (0, 0, 0, 0);

        var teamSkaters = gameSkaters.Where(s => s.TeamId == teamId);

        foreach (var skater in teamSkaters)
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

    private static (double AvgOffense, double AvgDefense) AverageSkaterScores(
        List<(int GameId, DateTime GameDate, DbGameSkaterStats Stats)> games, POSITION position)
    {
        double totalOff = 0, totalDef = 0;
        int count = 0;

        foreach (var (_, _, stats) in games)
        {
            if (stats.TimeOnIceSeconds == 0)
                continue;

            double toiMinutes60 = stats.TimeOnIceSeconds / 3600.0;

            double offense = (0.75 * stats.Goals + 0.63 * stats.Assists + 0.075 * stats.ShotsOnGoal
                + 0.15 * stats.PowerPlayGoals) / toiMinutes60;

            if (position == POSITION.Center)
                offense += 0.10 * stats.FaceOffWinningPctg;

            double defense = (0.10 * stats.BlockedShots + 0.08 * stats.Hits + 0.12 * stats.Takeaways
                + 0.05 * stats.PlusMinus - 0.075 * stats.PenaltyMinutes - 0.06 * stats.Giveaways) / toiMinutes60;

            totalOff += offense;
            totalDef += defense;
            count++;
        }

        if (count == 0)
            return (0, 0);

        return (totalOff / count, totalDef / count);
    }

    private (double SeasonGoalie, double RecentGoalie) ComputeGoalieValues(int gameId, int teamId, DateTime gameDate)
    {
        if (!_goalieStatsByGame.TryGetValue(gameId, out var gameGoalies))
            return (0, 0);

        // Find starter for this team, fall back to goalie with most TOI
        var teamGoalies = gameGoalies.Where(g => g.TeamId == teamId).ToList();
        var starter = teamGoalies.FirstOrDefault(g => g.IsStarter)
            ?? teamGoalies.OrderByDescending(g => g.TimeOnIceSeconds).FirstOrDefault();

        if (starter == null || !_goalieHistory.TryGetValue(starter.PlayerId, out var history))
            return (0, 0);

        var priorGames = history.Where(h => h.GameDate < gameDate).ToList();
        if (priorGames.Count == 0)
            return (0, 0);

        double seasonGoalie = AverageGoalieSavePct(priorGames);

        var recentGames = priorGames.TakeLast(RECENT_GAMES).ToList();
        double recentGoalie = AverageGoalieSavePct(recentGames);

        return (seasonGoalie, recentGoalie);
    }

    private static double AverageGoalieSavePct(List<(int GameId, DateTime GameDate, DbGameGoalieStats Stats)> games)
    {
        int totalSaves = 0, totalShots = 0;

        foreach (var (_, _, stats) in games)
        {
            int saves = stats.EvenStrengthShotsSaved + stats.PowerPlayShotsSaved + stats.ShortHandedShotsSaved;
            int goalsAllowed = stats.EvenStrengthGoalsAllowed + stats.PowerPlayGoalsAllowed + stats.ShortHandedGoalsAllowed;
            totalSaves += saves;
            totalShots += saves + goalsAllowed;
        }

        if (totalShots == 0)
            return 0;

        return (double)totalSaves / totalShots;
    }
}
