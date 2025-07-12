using Entities.DbModels;
using Entities.Models;

namespace Entities.Mappers.GameMappers;

public static class MapGameToDbGamePlayerStats
{
    public static IEnumerable<IDbGamePlayerStats> Map(Game game)
    {
        if (game.RosterStats == null)
            throw new ArgumentNullException(nameof(game.RosterStats), "Game roster stats can not be null.");

        var dbGamePlayerStats = new List<IDbGamePlayerStats>();
        var gameRosterStats = game.RosterStats;
        dbGamePlayerStats.AddRange(MapGameSkaterStatsToDbGameSkaterStats(gameRosterStats.AllPlayers.OfType<GameSkaterStats>(), game.Id));
        dbGamePlayerStats.AddRange(MapGameGoalieStatsToDbGameGoalieStats(gameRosterStats.AllPlayers.OfType<GameGoalieStats>(), game.Id));

        return dbGamePlayerStats;
    }

    public static IEnumerable<IDbGamePlayerStats> MapList(IEnumerable<Game> games)
    {
        var gamesPlayerStats = new List<IDbGamePlayerStats>();
        foreach (var game in games)
        {
            var gamePlayerStats = Map(game);
            gamesPlayerStats.AddRange(gamePlayerStats);
        }

        return gamesPlayerStats;
    }

    private static IEnumerable<IDbGamePlayerStats> MapGameGoalieStatsToDbGameGoalieStats(IEnumerable<GameGoalieStats> goalieStats, int gameId)
    {
        var dbGameStats = new List<DbGameGoalieStats>();
        foreach (var goalieStat in goalieStats)
        {
            dbGameStats.Add(MapSingleGameGoalieStatToDbGameGoalieStat(goalieStat, gameId));
        }
        return dbGameStats;
    }

    private static DbGameGoalieStats MapSingleGameGoalieStatToDbGameGoalieStat(GameGoalieStats goalieStat, int gameId)
    {
        return new DbGameGoalieStats
        {
            GameId = gameId,
            PlayerId = goalieStat.PlayerId,
            TeamId = goalieStat.TeamId,
            EvenStrengthShotsSaved = goalieStat.EvenStrengthShotsSaved,
            PowerPlayShotsSaved = goalieStat.PowerPlayShotsSaved,
            ShortHandedShotsSaved = goalieStat.ShortHandedShotsSaved,
            TimeOnIceSeconds = goalieStat.TimeOnIceSeconds,
            ShortHandedGoalsAllowed = goalieStat.ShortHandedGoalsAllowed,
            EvenStrengthGoalsAllowed = goalieStat.EvenStrengthGoalsAllowed,
            PowerPlayGoalsAllowed = goalieStat.PowerPlayGoalsAllowed,
            IsStarter = goalieStat.IsStarter,
            Position = goalieStat.Position,
        };
    }

    private static IEnumerable<IDbGamePlayerStats> MapGameSkaterStatsToDbGameSkaterStats(IEnumerable<GameSkaterStats> skaterStats, int gameId)
    {
        var dbGameStats = new List<DbGameSkaterStats>();
        foreach (var skaterStat in skaterStats)
        {
            dbGameStats.Add(MapSingleGameSkaterStatToDbGameSkaterStat(skaterStat, gameId));
        }
        return dbGameStats;
    }

    private static DbGameSkaterStats MapSingleGameSkaterStatToDbGameSkaterStat(GameSkaterStats skaterStat, int gameId)
    {
        return new DbGameSkaterStats()
        {
            GameId = gameId,
            PlayerId = skaterStat.PlayerId,
            TeamId = skaterStat.TeamId,
            Goals = skaterStat.Goals,
            Assists = skaterStat.Assists,
            PlusMinus = skaterStat.PlusMinus,
            PenaltyMinutes = skaterStat.PenaltyMinutes,
            Hits = skaterStat.Hits,
            PowerPlayGoals = skaterStat.PowerPlayGoals,
            ShotsOnGoal = skaterStat.ShotsOnGoal,
            FaceOffWinningPctg = skaterStat.FaceOffWinningPctg,
            BlockedShots = skaterStat.BlockedShots,
            Giveaways = skaterStat.Giveaways,
            Takeaways = skaterStat.Takeaways,
            TimeOnIceSeconds = skaterStat.TimeOnIceSeconds,
            Position = skaterStat.Position,
        };
    }
}

