using Entities.DbModels;
using Entities.Models;

namespace Entities.Mappers.GameMappers;

public static class MapDbGamePlayerStatsToGamePlayerStats
{
    public static GameSkaterStats MapSkaterStats(DbGameSkaterStats dbGameSkaterStats)
    {
        return new GameSkaterStats
        {
            PlayerId = dbGameSkaterStats.PlayerId,
            TeamId = dbGameSkaterStats.TeamId,
            TimeOnIceSeconds = dbGameSkaterStats.TimeOnIceSeconds,
            Position = dbGameSkaterStats.Position,
            Goals = dbGameSkaterStats.Goals,
            Assists = dbGameSkaterStats.Assists,
            PenaltyMinutes = dbGameSkaterStats.PenaltyMinutes,
            ShotsOnGoal = dbGameSkaterStats.ShotsOnGoal,
            Hits = dbGameSkaterStats.Hits,
            PowerPlayGoals = dbGameSkaterStats.PowerPlayGoals,
            PlusMinus = dbGameSkaterStats.PlusMinus,
            FaceOffWinningPctg = dbGameSkaterStats.FaceOffWinningPctg,
            BlockedShots = dbGameSkaterStats.BlockedShots,
            Giveaways = dbGameSkaterStats.Giveaways,
            Takeaways = dbGameSkaterStats.Takeaways
        };
    }

    public static IEnumerable<GameSkaterStats> MapSkaterStatsList(IEnumerable<DbGameSkaterStats> dbGameSkaterStats)
    {
        var gameSkaterStatsList = new List<GameSkaterStats>();
        foreach (var dbGameSkaterStat in dbGameSkaterStats)
        {
            gameSkaterStatsList.Add(MapSkaterStats(dbGameSkaterStat));
        }

        return gameSkaterStatsList;
    }

    public static GameGoalieStats MapGoalieStats(DbGameGoalieStats dbGameGoalieStats)
    {
        return new GameGoalieStats
        {
            PlayerId = dbGameGoalieStats.PlayerId,
            TeamId = dbGameGoalieStats.TeamId,
            EvenStrengthShotsSaved = dbGameGoalieStats.EvenStrengthShotsSaved,
            PowerPlayShotsSaved = dbGameGoalieStats.PowerPlayShotsSaved,
            ShortHandedShotsSaved = dbGameGoalieStats.ShortHandedShotsSaved,
            EvenStrengthGoalsAllowed = dbGameGoalieStats.EvenStrengthGoalsAllowed,
            PowerPlayGoalsAllowed = dbGameGoalieStats.PowerPlayGoalsAllowed,
            ShortHandedGoalsAllowed = dbGameGoalieStats.ShortHandedGoalsAllowed,
            TimeOnIceSeconds = dbGameGoalieStats.TimeOnIceSeconds,
            IsStarter = dbGameGoalieStats.IsStarter,
            Position = dbGameGoalieStats.Position
        };
    }

    public static IEnumerable<GameGoalieStats> MapGoalieStatsList(IEnumerable<DbGameGoalieStats> dbGameGoalieStats)
    {
        var gameGoalieStatsList = new List<GameGoalieStats>();
        foreach (var dbGameGoalieStat in dbGameGoalieStats)
        {
            gameGoalieStatsList.Add(MapGoalieStats(dbGameGoalieStat));
        }

        return gameGoalieStatsList;
    }
}

