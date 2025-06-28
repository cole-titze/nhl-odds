using Entities.Models;

namespace Entities.DbModels.Mappers
{
    public static class MapGameToDbGamePlayerStats
    {
        public static IEnumerable<IDbGamePlayerStats> Map(Game game)
        {
            if (game.rosterStats == null)
                throw new ArgumentNullException(nameof(game.rosterStats), "Game roster stats can not be null.");

            var dbGamePlayerStats = new List<IDbGamePlayerStats>();
            var gameRosterStats = game.rosterStats;
            dbGamePlayerStats.AddRange(MapGameSkaterStatsToDbGameSkaterStats(gameRosterStats.AllPlayers.OfType<GameSkaterStats>(), game.id));
            dbGamePlayerStats.AddRange(MapGameGoalieStatsToDbGameGoalieStats(gameRosterStats.AllPlayers.OfType<GameGoalieStats>(), game.id));

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
                gameId = gameId,
                playerId = goalieStat.playerId,
                teamId = goalieStat.teamId,
                evenStrengthShotsSaved = goalieStat.evenStrengthShotsSaved,
                powerPlayShotsSaved = goalieStat.powerPlayShotsSaved,
                shortHandedShotsSaved = goalieStat.shortHandedShotsSaved,
                timeOnIceSeconds = goalieStat.timeOnIceSeconds,
                shortHandedGoalsAllowed = goalieStat.shortHandedGoalsAllowed,
                evenStrengthGoalsAllowed = goalieStat.evenStrengthGoalsAllowed,
                powerPlayGoalsAllowed = goalieStat.powerPlayGoalsAllowed,
                isStarter = goalieStat.isStarter,
                position = goalieStat.position,
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
                gameId = gameId,
                playerId = skaterStat.playerId,
                teamId = skaterStat.TeamId,
                goals = skaterStat.Goals,
                assists = skaterStat.Assists,
                plusMinus = skaterStat.PlusMinus,
                penaltyMinutes = skaterStat.PenaltyMinutes,
                hits = skaterStat.Hits,
                powerPlayGoals = skaterStat.PowerPlayGoals,
                shotsOnGoal = skaterStat.ShotsOnGoal,
                faceOffWinningPctg = skaterStat.FaceOffWinningPctg,
                blockedShots = skaterStat.BlockedShots,
                giveaways = skaterStat.Giveaways,
                takeaways = skaterStat.Takeaways,
                timeOnIceSeconds = skaterStat.TimeOnIceSeconds,
                position = skaterStat.Position,
            };
        }
    }
}

