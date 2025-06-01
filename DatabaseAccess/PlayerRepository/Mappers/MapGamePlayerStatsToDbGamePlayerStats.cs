using Entities.DbModels;
using Entities.Models;

namespace DataAccess.PlayerRepository.Mappers
{
    public static class MapGamePlayerStatsToDbGamePlayerStats
    {
        public static IEnumerable<IDbGamePlayerStats> Map(IEnumerable<IGamePlayerStats> gamePlayersStats)
		{
            var dbGamePlayersStats = new List<IDbGamePlayerStats>();
            var gameSkaterStats = MapGameSkaterStatsToDbGameSkaterStats(gamePlayersStats.OfType<GameSkaterStats>());
            var gameGoalieStats = MapGameGoalieStatsToDbGameGoalieStats(gamePlayersStats.OfType<GameGoalieStats>());

            return gameSkaterStats.Concat(gameGoalieStats);
		}

        private static IEnumerable<IDbGamePlayerStats> MapGameGoalieStatsToDbGameGoalieStats(IEnumerable<GameGoalieStats> goalieStats)
        {
            var dbGameStats = new List<DbGameGoalieStats>();
            foreach (var goalieStat in goalieStats)
            {
                dbGameStats.Add(MapSingleGameGoalieStatToDbGameGoalieStat(goalieStat));
            }
            return dbGameStats;
        }

        private static DbGameGoalieStats MapSingleGameGoalieStatToDbGameGoalieStat(GameGoalieStats goalieStat)
        {
            return new DbGameGoalieStats
            {
                gameId = goalieStat.gameId,
                playerId = goalieStat.playerId,
                teamId = goalieStat.teamId,
                evenStrengthShotsSaved = goalieStat.evenStrengthShotsSaved,
                powerPlayShotsSaved = goalieStat.powerPlayShotsSaved,
                evenStrengthGoalsAllowed = goalieStat.evenStrengthGoalsAllowed,
                powerPlayGoalsAllowed = goalieStat.powerPlayGoalsAllowed,
                isStarter = goalieStat.isStarter,
                position = goalieStat.position,
            };
        }

        private static IEnumerable<IDbGamePlayerStats> MapGameSkaterStatsToDbGameSkaterStats(IEnumerable<GameSkaterStats> skaterStats)
        {
            var dbGameStats = new List<DbGameSkaterStats>();
            foreach (var skaterStat in skaterStats)
            {
                dbGameStats.Add(MapSingleGameSkaterStatToDbGameSkaterStat(skaterStat));
            }
            return dbGameStats;
        }

        private static DbGameSkaterStats MapSingleGameSkaterStatToDbGameSkaterStat(GameSkaterStats skaterStat)
        {
            return new DbGameSkaterStats()
            {
                gameId = skaterStat.gameId,
                playerId = skaterStat.playerId,
                teamId = skaterStat.teamId,
                goals = skaterStat.goals,
                assists = skaterStat.assists,
                plusMinus = skaterStat.plusMinus,
                penaltyMinutes = skaterStat.penaltyMinutes,
                hits = skaterStat.hits,
                powerPlayGoals = skaterStat.powerPlayGoals,
                shotsOnGoal = skaterStat.shotsOnGoal,
                faceOffWinningPctg = skaterStat.faceOffWinningPctg,
                blockedShots = skaterStat.blockedShots,
                giveaways = skaterStat.giveaways,
                takeaways = skaterStat.takeaways,
                timeOnIceSeconds = skaterStat.timeOnIceSeconds,
                position = skaterStat.position
            };
        }
    }
}

