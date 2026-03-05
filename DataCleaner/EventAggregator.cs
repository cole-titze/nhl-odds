using Entities.DbModels.GamePlayEvents;
using Entities.Models;
using Entities.Types.Enums;
using Zone = Entities.Types.Enums.Zone;

namespace DataCleaner;

public record TeamEventValues(
    double PpEfficiency,
    double RecentPpEfficiency,
    double PkEfficiency,
    double RecentPkEfficiency,
    double GoalsPerGamePeriod1,
    double GoalsPerGamePeriod2,
    double GoalsPerGamePeriod3,
    double RecentGoalsPerGamePeriod1,
    double RecentGoalsPerGamePeriod2,
    double RecentGoalsPerGamePeriod3,
    double OffensiveZoneFaceoffWinPct,
    double RecentOffensiveZoneFaceoffWinPct,
    double PenaltyDifferentialAvg,
    double RecentPenaltyDifferentialAvg,
    double CorsiPct,
    double RecentCorsiPct
);

public class EventAggregator
{
    private const int RECENT_GAMES = 5;

    private static readonly HashSet<PenaltySeverity> PpPkSeverities = new()
    {
        PenaltySeverity.Minor,
        PenaltySeverity.Major,
        PenaltySeverity.Bench
    };

    // gameId -> list of penalties for that game
    private readonly Dictionary<int, List<DbPenalty>> _penaltiesByGame;
    // gameId -> list of goals for that game
    private readonly Dictionary<int, List<DbGoal>> _goalsByGame;
    // gameId -> list of faceoffs for that game
    private readonly Dictionary<int, List<DbFaceoff>> _faceoffsByGame;
    // gameId -> list of missed shots for that game
    private readonly Dictionary<int, List<DbMissedShot>> _missedShotsByGame;
    // gameId -> (homeTeamId, awayTeamId, gameDate)
    private readonly Dictionary<int, (int HomeTeamId, int AwayTeamId, DateTime GameDate)> _gameTeams;
    // gameId -> (homePPG, awayPPG)
    private readonly Dictionary<int, (int HomePPG, int AwayPPG)> _ppgByGame;
    // gameId -> (homeSOG, awaySOG, homeBlocked, awayBlocked)
    private readonly Dictionary<int, (int HomeSOG, int AwaySOG, int HomeBlocked, int AwayBlocked)> _shotStatsByGame;

    public EventAggregator(
        IEnumerable<DbPenalty> penalties,
        IEnumerable<DbGoal> goals,
        IEnumerable<DbFaceoff> faceoffs,
        IEnumerable<DbMissedShot> missedShots,
        IEnumerable<Game> games)
    {
        _gameTeams = new Dictionary<int, (int, int, DateTime)>();
        _ppgByGame = new Dictionary<int, (int, int)>();
        _shotStatsByGame = new Dictionary<int, (int, int, int, int)>();
        foreach (var game in games)
        {
            _gameTeams[game.Id] = (game.HomeTeamId, game.AwayTeamId, game.GameDateUTC);
            _ppgByGame[game.Id] = (game.HomePPG, game.AwayPPG);
            _shotStatsByGame[game.Id] = (game.HomeSOG, game.AwaySOG, game.HomeBlockedShots, game.AwayBlockedShots);
        }

        _penaltiesByGame = new Dictionary<int, List<DbPenalty>>();
        foreach (var penalty in penalties)
        {
            if (!_gameTeams.ContainsKey(penalty.GameId))
                continue;
            if (!_penaltiesByGame.ContainsKey(penalty.GameId))
                _penaltiesByGame[penalty.GameId] = new List<DbPenalty>();
            _penaltiesByGame[penalty.GameId].Add(penalty);
        }

        _goalsByGame = new Dictionary<int, List<DbGoal>>();
        foreach (var goal in goals)
        {
            if (!_gameTeams.ContainsKey(goal.GameId))
                continue;
            if (!_goalsByGame.ContainsKey(goal.GameId))
                _goalsByGame[goal.GameId] = new List<DbGoal>();
            _goalsByGame[goal.GameId].Add(goal);
        }

        _faceoffsByGame = new Dictionary<int, List<DbFaceoff>>();
        foreach (var faceoff in faceoffs)
        {
            if (!_gameTeams.ContainsKey(faceoff.GameId))
                continue;
            if (!_faceoffsByGame.ContainsKey(faceoff.GameId))
                _faceoffsByGame[faceoff.GameId] = new List<DbFaceoff>();
            _faceoffsByGame[faceoff.GameId].Add(faceoff);
        }

        _missedShotsByGame = new Dictionary<int, List<DbMissedShot>>();
        foreach (var missedShot in missedShots)
        {
            if (!_gameTeams.ContainsKey(missedShot.GameId))
                continue;
            if (!_missedShotsByGame.ContainsKey(missedShot.GameId))
                _missedShotsByGame[missedShot.GameId] = new List<DbMissedShot>();
            _missedShotsByGame[missedShot.GameId].Add(missedShot);
        }
    }

    public TeamEventValues? GetTeamEventValues(int gameId, int teamId)
    {
        if (!_gameTeams.TryGetValue(gameId, out var gameInfo))
            return null;

        var priorGameIds = GetPriorGameIds(gameId, teamId, gameInfo.GameDate);
        if (priorGameIds.Count == 0)
            return null;

        var recentGameIds = priorGameIds.TakeLast(RECENT_GAMES).ToList();

        return new TeamEventValues(
            PpEfficiency: ComputePpEfficiency(priorGameIds, teamId),
            RecentPpEfficiency: ComputePpEfficiency(recentGameIds, teamId),
            PkEfficiency: ComputePkEfficiency(priorGameIds, teamId),
            RecentPkEfficiency: ComputePkEfficiency(recentGameIds, teamId),
            GoalsPerGamePeriod1: ComputePeriodGoalsPerGame(priorGameIds, teamId, 1),
            GoalsPerGamePeriod2: ComputePeriodGoalsPerGame(priorGameIds, teamId, 2),
            GoalsPerGamePeriod3: ComputePeriodGoalsPerGame(priorGameIds, teamId, 3),
            RecentGoalsPerGamePeriod1: ComputePeriodGoalsPerGame(recentGameIds, teamId, 1),
            RecentGoalsPerGamePeriod2: ComputePeriodGoalsPerGame(recentGameIds, teamId, 2),
            RecentGoalsPerGamePeriod3: ComputePeriodGoalsPerGame(recentGameIds, teamId, 3),
            OffensiveZoneFaceoffWinPct: ComputeOffensiveZoneFaceoffWinPct(priorGameIds, teamId),
            RecentOffensiveZoneFaceoffWinPct: ComputeOffensiveZoneFaceoffWinPct(recentGameIds, teamId),
            PenaltyDifferentialAvg: ComputePenaltyDifferentialAvg(priorGameIds, teamId),
            RecentPenaltyDifferentialAvg: ComputePenaltyDifferentialAvg(recentGameIds, teamId),
            CorsiPct: ComputeCorsiPct(priorGameIds, teamId),
            RecentCorsiPct: ComputeCorsiPct(recentGameIds, teamId)
        );
    }

    private List<int> GetPriorGameIds(int gameId, int teamId, DateTime gameDate)
    {
        return _gameTeams
            .Where(kvp => kvp.Key != gameId
                && kvp.Value.GameDate < gameDate
                && (kvp.Value.HomeTeamId == teamId || kvp.Value.AwayTeamId == teamId))
            .OrderBy(kvp => kvp.Value.GameDate)
            .Select(kvp => kvp.Key)
            .ToList();
    }

    private double ComputePpEfficiency(List<int> gameIds, int teamId)
    {
        int totalPPG = 0;
        int totalPPOpportunities = 0;

        foreach (var gid in gameIds)
        {
            if (!_gameTeams.TryGetValue(gid, out var info))
                continue;

            // Get PPG from Game model
            if (_ppgByGame.TryGetValue(gid, out var ppg))
            {
                totalPPG += info.HomeTeamId == teamId ? ppg.HomePPG : ppg.AwayPPG;
            }

            // PP opportunities = opponent penalties with qualifying severity
            if (_penaltiesByGame.TryGetValue(gid, out var penalties))
            {
                int opponentId = info.HomeTeamId == teamId ? info.AwayTeamId : info.HomeTeamId;
                totalPPOpportunities += penalties.Count(p =>
                    p.CommittedByPlayerTeamId == opponentId && PpPkSeverities.Contains(p.PenaltySeverity));
            }
        }

        if (totalPPOpportunities == 0)
            return 0;
        return (double)totalPPG / totalPPOpportunities;
    }

    private double ComputePkEfficiency(List<int> gameIds, int teamId)
    {
        int totalOpponentPPG = 0;
        int totalPKTimes = 0;

        foreach (var gid in gameIds)
        {
            if (!_gameTeams.TryGetValue(gid, out var info))
                continue;

            int opponentId = info.HomeTeamId == teamId ? info.AwayTeamId : info.HomeTeamId;

            // Opponent PPG from Game model
            if (_ppgByGame.TryGetValue(gid, out var ppg))
            {
                totalOpponentPPG += info.HomeTeamId == teamId ? ppg.AwayPPG : ppg.HomePPG;
            }

            // PK times = own penalties with qualifying severity
            if (_penaltiesByGame.TryGetValue(gid, out var penalties))
            {
                totalPKTimes += penalties.Count(p =>
                    p.CommittedByPlayerTeamId == teamId && PpPkSeverities.Contains(p.PenaltySeverity));
            }
        }

        if (totalPKTimes == 0)
            return 1;
        return 1.0 - ((double)totalOpponentPPG / totalPKTimes);
    }

    private double ComputePeriodGoalsPerGame(List<int> gameIds, int teamId, int period)
    {
        int totalGoals = 0;
        int gameCount = gameIds.Count;

        if (gameCount == 0)
            return 0;

        foreach (var gid in gameIds)
        {
            if (_goalsByGame.TryGetValue(gid, out var goals))
            {
                totalGoals += goals.Count(g =>
                    g.ScoringPlayerTeamId == teamId && g.PeriodNumber == period);
            }
        }

        return (double)totalGoals / gameCount;
    }

    private double ComputeOffensiveZoneFaceoffWinPct(List<int> gameIds, int teamId)
    {
        int offZoneWins = 0;
        int totalWins = 0;

        foreach (var gid in gameIds)
        {
            if (!_faceoffsByGame.TryGetValue(gid, out var faceoffs))
                continue;

            foreach (var fo in faceoffs)
            {
                if (fo.WinningTeamId != teamId)
                    continue;

                totalWins++;
                // Zone is from winning team's perspective per NHL API
                if (fo.Zone == Zone.Offensive)
                    offZoneWins++;
            }
        }

        if (totalWins == 0)
            return 0;
        return (double)offZoneWins / totalWins;
    }

    private double ComputePenaltyDifferentialAvg(List<int> gameIds, int teamId)
    {
        int gameCount = gameIds.Count;
        if (gameCount == 0)
            return 0;

        double totalDiff = 0;

        foreach (var gid in gameIds)
        {
            if (!_gameTeams.TryGetValue(gid, out var info))
                continue;

            int opponentId = info.HomeTeamId == teamId ? info.AwayTeamId : info.HomeTeamId;

            int ownPenalties = 0;
            int opponentPenalties = 0;

            if (_penaltiesByGame.TryGetValue(gid, out var penalties))
            {
                foreach (var p in penalties)
                {
                    if (!PpPkSeverities.Contains(p.PenaltySeverity))
                        continue;
                    if (p.CommittedByPlayerTeamId == teamId)
                        ownPenalties++;
                    else if (p.CommittedByPlayerTeamId == opponentId)
                        opponentPenalties++;
                }
            }

            totalDiff += opponentPenalties - ownPenalties;
        }

        return totalDiff / gameCount;
    }

    private double ComputeCorsiPct(List<int> gameIds, int teamId)
    {
        // Corsi For = team SOG + team missed shots + opponent blocked shots
        // Corsi Against = opponent SOG + opponent missed shots + team blocked shots
        int totalCF = 0;
        int totalCA = 0;

        foreach (var gid in gameIds)
        {
            if (!_gameTeams.TryGetValue(gid, out var info))
                continue;

            bool isHome = info.HomeTeamId == teamId;

            if (_shotStatsByGame.TryGetValue(gid, out var shots))
            {
                int teamSOG = isHome ? shots.HomeSOG : shots.AwaySOG;
                int oppSOG = isHome ? shots.AwaySOG : shots.HomeSOG;
                // "Blocked shots" in NHL stats = shots blocked by the team (defensive)
                // So opponent's blocked shots = shots the team attempted that were blocked
                int oppBlocked = isHome ? shots.AwayBlocked : shots.HomeBlocked;
                int teamBlocked = isHome ? shots.HomeBlocked : shots.AwayBlocked;

                totalCF += teamSOG + oppBlocked;
                totalCA += oppSOG + teamBlocked;
            }

            // Add missed shots from play-by-play events
            if (_missedShotsByGame.TryGetValue(gid, out var missedShots))
            {
                int teamMissed = missedShots.Count(m => m.ShootingTeamId == teamId);
                int oppMissed = missedShots.Count(m => m.ShootingTeamId != teamId);
                totalCF += teamMissed;
                totalCA += oppMissed;
            }
        }

        int total = totalCF + totalCA;
        if (total == 0)
            return 0;
        return (double)totalCF / total;
    }
}
