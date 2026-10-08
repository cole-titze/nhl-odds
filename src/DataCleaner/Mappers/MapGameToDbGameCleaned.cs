using Entities.DbModels;
using Entities.Models;
using Entities.Types.Enums;

namespace DataCleaner.Mappers;

public static class MapGameToDbGameCleaned
{
    private const int RECENT_GAMES = 5;
    // Season-to-date stats use at least this many games, borrowing the most recent ones from last season
    private const int MIN_SEASON_GAMES = 10;
    // Longer rests (season openers, breaks) are treated the same
    private const double MAX_HOURS_SINCE_LAST_GAME = 120;

    public static DbGameCleaned Map(Game game, SeasonGames seasonGames, RosterScorer? rosterScorer = null)
    {
        var homeTeamGames = seasonGames.GamesMap[game.HomeTeamId];
        var awayTeamGames = seasonGames.GamesMap[game.AwayTeamId];
        // Lists of team games for current season
        var homeTeamSeasonGames = homeTeamGames.CurrentSeasonGames.PlayedBefore(game.GameDateUTC);
        var awayTeamSeasonGames = awayTeamGames.CurrentSeasonGames.PlayedBefore(game.GameDateUTC);
        // Season-to-date averages, topped up with last season's games early in the season so they don't start at 0
        var homeTeamSeasonStatGames = TopUpSeasonGames(homeTeamSeasonGames, homeTeamGames.Games.PlayedBefore(game.GameDateUTC));
        var awayTeamSeasonStatGames = TopUpSeasonGames(awayTeamSeasonGames, awayTeamGames.Games.PlayedBefore(game.GameDateUTC));
        // List of recently played team games
        var homeTeamRecentGames = homeTeamGames.Games.PlayedBefore(game.GameDateUTC).Take(RECENT_GAMES);
        var awayTeamRecentGames = awayTeamGames.Games.PlayedBefore(game.GameDateUTC).Take(RECENT_GAMES);
        // List of team games played that match current home/away position
        var homeTeamHomeGames = homeTeamGames.HomeGames.PlayedBefore(game.GameDateUTC);
        var awayTeamAwayGames = awayTeamGames.AwayGames.PlayedBefore(game.GameDateUTC);
        // List of recent team games played that match current home/away position
        var homeTeamRecentHomeGames = homeTeamGames.HomeGames.PlayedBefore(game.GameDateUTC).Take(RECENT_GAMES);
        var awayTeamRecentAwayGames = awayTeamGames.AwayGames.PlayedBefore(game.GameDateUTC).Take(RECENT_GAMES);

        // Head-to-head games between these two teams this season
        var headToHeadGames = homeTeamSeasonGames.Where(g =>
            g.HomeTeamId == game.AwayTeamId || g.AwayTeamId == game.AwayTeamId);

        var homeRoster = rosterScorer?.GetTeamRosterValues(game.Id, game.HomeTeamId);
        var awayRoster = rosterScorer?.GetTeamRosterValues(game.Id, game.AwayTeamId);

        var cleanedGame = new DbGameCleaned()
        {
            GameId = game.Id,

            HomeWinRatio = GetWinRatioOfGames(homeTeamSeasonStatGames, game.HomeTeamId),
            HomeRecentWinRatio = GetWinRatioOfGames(homeTeamRecentGames, game.HomeTeamId),
            HomeGoalsAvg = GetStatAvg(homeTeamSeasonStatGames, game.HomeTeamId, g => g.HomeGoals, g => g.AwayGoals),
            HomeRecentGoalsAvg = GetStatAvg(homeTeamRecentGames, game.HomeTeamId, g => g.HomeGoals, g => g.AwayGoals),
            HomeConcededGoalsAvg = GetStatAvg(homeTeamSeasonStatGames, game.HomeTeamId, g => g.AwayGoals, g => g.HomeGoals),
            HomeRecentConcededGoalsAvg = GetStatAvg(homeTeamRecentGames, game.HomeTeamId, g => g.AwayGoals, g => g.HomeGoals),
            HomeRecentSogAvg = GetStatAvg(homeTeamRecentGames, game.HomeTeamId, g => g.HomeSOG, g => g.AwaySOG),
            HomeRecentBlockedShotsAvg = GetStatAvg(homeTeamRecentGames, game.HomeTeamId, g => g.HomeBlockedShots, g => g.AwayBlockedShots),
            HomeRecentPpgAvg = GetStatAvg(homeTeamRecentGames, game.HomeTeamId, g => g.HomePPG, g => g.AwayPPG),
            HomeRecentHitsAvg = GetStatAvg(homeTeamRecentGames, game.HomeTeamId, g => g.HomeHits, g => g.AwayHits),
            HomeRecentPimAvg = GetStatAvg(homeTeamRecentGames, game.HomeTeamId, g => g.HomePIM, g => g.AwayPIM),
            HomeRecentTakeawaysAvg = GetStatAvg(homeTeamRecentGames, game.HomeTeamId, g => g.HomeTakeaways, g => g.AwayTakeaways),
            HomeRecentGiveawaysAvg = GetStatAvg(homeTeamRecentGames, game.HomeTeamId, g => g.HomeGiveaways, g => g.AwayGiveaways),
            HomeConcededGoalsAvgAtHome = GetStatAvg(homeTeamHomeGames, game.HomeTeamId, g => g.AwayGoals, g => g.HomeGoals),
            HomeRecentConcededGoalsAvgAtHome = GetStatAvg(homeTeamHomeGames.Take(RECENT_GAMES), game.HomeTeamId, g => g.AwayGoals, g => g.HomeGoals),
            HomeGoalsAvgAtHome = GetStatAvg(homeTeamHomeGames, game.HomeTeamId, g => g.HomeGoals, g => g.AwayGoals),
            HomeRecentGoalsAvgAtHome = GetStatAvg(homeTeamRecentHomeGames, game.HomeTeamId, g => g.HomeGoals, g => g.AwayGoals),
            HomeHoursSinceLastGame = Math.Min(game.GetHoursBetweenGames(homeTeamSeasonGames.FirstOrDefault()), MAX_HOURS_SINCE_LAST_GAME),
            HomeRosterOffenseValue = homeRoster?.RosterOffenseValue ?? 0,
            HomeRosterDefenseValue = homeRoster?.RosterDefenseValue ?? 0,
            HomeRosterGoalieValue = homeRoster?.RosterGoalieValue ?? 0,
            HomeRecentRosterOffenseValue = homeRoster?.RecentRosterOffenseValue ?? 0,
            HomeRecentRosterDefenseValue = homeRoster?.RecentRosterDefenseValue ?? 0,
            HomeRecentRosterGoalieValue = homeRoster?.RecentRosterGoalieValue ?? 0,

            AwayWinRatio = GetWinRatioOfGames(awayTeamSeasonStatGames, game.AwayTeamId),
            AwayRecentWinRatio = GetWinRatioOfGames(awayTeamRecentGames, game.AwayTeamId),
            AwayGoalsAvg = GetStatAvg(awayTeamSeasonStatGames, game.AwayTeamId, g => g.HomeGoals, g => g.AwayGoals),
            AwayRecentGoalsAvg = GetStatAvg(awayTeamRecentGames, game.AwayTeamId, g => g.HomeGoals, g => g.AwayGoals),
            AwayConcededGoalsAvg = GetStatAvg(awayTeamSeasonStatGames, game.AwayTeamId, g => g.AwayGoals, g => g.HomeGoals),
            AwayRecentConcededGoalsAvg = GetStatAvg(awayTeamRecentGames, game.AwayTeamId, g => g.AwayGoals, g => g.HomeGoals),
            AwayRecentSogAvg = GetStatAvg(awayTeamRecentGames, game.AwayTeamId, g => g.HomeSOG, g => g.AwaySOG),
            AwayRecentBlockedShotsAvg = GetStatAvg(awayTeamRecentGames, game.AwayTeamId, g => g.HomeBlockedShots, g => g.AwayBlockedShots),
            AwayRecentPpgAvg = GetStatAvg(awayTeamRecentGames, game.AwayTeamId, g => g.HomePPG, g => g.AwayPPG),
            AwayRecentHitsAvg = GetStatAvg(awayTeamRecentGames, game.AwayTeamId, g => g.HomeHits, g => g.AwayHits),
            AwayRecentPimAvg = GetStatAvg(awayTeamRecentGames, game.AwayTeamId, g => g.HomePIM, g => g.AwayPIM),
            AwayRecentTakeawaysAvg = GetStatAvg(awayTeamRecentGames, game.AwayTeamId, g => g.HomeTakeaways, g => g.AwayTakeaways),
            AwayRecentGiveawaysAvg = GetStatAvg(awayTeamRecentGames, game.AwayTeamId, g => g.HomeGiveaways, g => g.AwayGiveaways),
            AwayConcededGoalsAvgAtAway = GetStatAvg(awayTeamAwayGames, game.AwayTeamId, g => g.AwayGoals, g => g.HomeGoals),
            AwayRecentConcededGoalsAvgAtAway = GetStatAvg(awayTeamRecentAwayGames, game.AwayTeamId, g => g.AwayGoals, g => g.HomeGoals),
            AwayGoalsAvgAtAway = GetStatAvg(awayTeamAwayGames, game.AwayTeamId, g => g.HomeGoals, g => g.AwayGoals),
            AwayRecentGoalsAvgAtAway = GetStatAvg(awayTeamRecentAwayGames, game.AwayTeamId, g => g.HomeGoals, g => g.AwayGoals),
            AwayHoursSinceLastGame = Math.Min(game.GetHoursBetweenGames(awayTeamSeasonGames.FirstOrDefault()), MAX_HOURS_SINCE_LAST_GAME),
            AwayRosterOffenseValue = awayRoster?.RosterOffenseValue ?? 0,
            AwayRosterDefenseValue = awayRoster?.RosterDefenseValue ?? 0,
            AwayRosterGoalieValue = awayRoster?.RosterGoalieValue ?? 0,
            AwayRecentRosterOffenseValue = awayRoster?.RecentRosterOffenseValue ?? 0,
            AwayRecentRosterDefenseValue = awayRoster?.RecentRosterDefenseValue ?? 0,
            AwayRecentRosterGoalieValue = awayRoster?.RecentRosterGoalieValue ?? 0,
        };

        cleanedGame.HomeIsBackToBack = cleanedGame.HomeHoursSinceLastGame <= 28 ? 1.0 : 0.0;
        cleanedGame.AwayIsBackToBack = cleanedGame.AwayHoursSinceLastGame <= 28 ? 1.0 : 0.0;
        cleanedGame.RestAdvantage = cleanedGame.HomeHoursSinceLastGame - cleanedGame.AwayHoursSinceLastGame;

        cleanedGame.HomeRecentShotAttemptsAvg = GetStatAvg(homeTeamRecentGames, game.HomeTeamId,
            g => g.HomeSOG + g.AwayBlockedShots, g => g.AwaySOG + g.HomeBlockedShots);
        cleanedGame.AwayRecentShotAttemptsAvg = GetStatAvg(awayTeamRecentGames, game.AwayTeamId,
            g => g.HomeSOG + g.AwayBlockedShots, g => g.AwaySOG + g.HomeBlockedShots);

        cleanedGame.HomeGoalDiffAvg = GetGoalDiffAvg(homeTeamSeasonStatGames, game.HomeTeamId);
        cleanedGame.AwayGoalDiffAvg = GetGoalDiffAvg(awayTeamSeasonStatGames, game.AwayTeamId);
        cleanedGame.HomeRecentGoalDiffAvg = GetGoalDiffAvg(homeTeamRecentGames, game.HomeTeamId);
        cleanedGame.AwayRecentGoalDiffAvg = GetGoalDiffAvg(awayTeamRecentGames, game.AwayTeamId);

        cleanedGame.HomeStreak = GetStreak(homeTeamSeasonGames, game.HomeTeamId);
        cleanedGame.AwayStreak = GetStreak(awayTeamSeasonGames, game.AwayTeamId);

        cleanedGame.HomeWinRatioAtHome = GetWinRatioOfGames(homeTeamHomeGames, game.HomeTeamId);
        cleanedGame.AwayWinRatioAtAway = GetWinRatioOfGames(awayTeamAwayGames, game.AwayTeamId);
        cleanedGame.HeadToHeadWinRatio = GetWinRatioOfGames(headToHeadGames, game.HomeTeamId);

        if (rosterScorer != null)
        {
            (cleanedGame.HomeSavePct, cleanedGame.HomeRecentSavePct) = rosterScorer.GetTeamSavePct(game.Id, game.HomeTeamId);
            (cleanedGame.AwaySavePct, cleanedGame.AwayRecentSavePct) = rosterScorer.GetTeamSavePct(game.Id, game.AwayTeamId);
        }
        else
        {
            cleanedGame.HomeSavePct = GetSavePct(homeTeamSeasonStatGames, game.HomeTeamId);
            cleanedGame.AwaySavePct = GetSavePct(awayTeamSeasonStatGames, game.AwayTeamId);
            cleanedGame.HomeRecentSavePct = GetSavePct(homeTeamRecentGames, game.HomeTeamId);
            cleanedGame.AwayRecentSavePct = GetSavePct(awayTeamRecentGames, game.AwayTeamId);
        }

        cleanedGame.HomeSogAvg = GetStatAvg(homeTeamSeasonStatGames, game.HomeTeamId, g => g.HomeSOG, g => g.AwaySOG);
        cleanedGame.AwaySogAvg = GetStatAvg(awayTeamSeasonStatGames, game.AwayTeamId, g => g.HomeSOG, g => g.AwaySOG);
        cleanedGame.HomePpgAvg = GetStatAvg(homeTeamSeasonStatGames, game.HomeTeamId, g => g.HomePPG, g => g.AwayPPG);
        cleanedGame.AwayPpgAvg = GetStatAvg(awayTeamSeasonStatGames, game.AwayTeamId, g => g.HomePPG, g => g.AwayPPG);
        cleanedGame.HomeHitsAvg = GetStatAvg(homeTeamSeasonStatGames, game.HomeTeamId, g => g.HomeHits, g => g.AwayHits);
        cleanedGame.AwayHitsAvg = GetStatAvg(awayTeamSeasonStatGames, game.AwayTeamId, g => g.HomeHits, g => g.AwayHits);
        cleanedGame.HomePimAvg = GetStatAvg(homeTeamSeasonStatGames, game.HomeTeamId, g => g.HomePIM, g => g.AwayPIM);
        cleanedGame.AwayPimAvg = GetStatAvg(awayTeamSeasonStatGames, game.AwayTeamId, g => g.HomePIM, g => g.AwayPIM);
        cleanedGame.HomeBlockedShotsAvg = GetStatAvg(homeTeamSeasonStatGames, game.HomeTeamId, g => g.HomeBlockedShots, g => g.AwayBlockedShots);
        cleanedGame.AwayBlockedShotsAvg = GetStatAvg(awayTeamSeasonStatGames, game.AwayTeamId, g => g.HomeBlockedShots, g => g.AwayBlockedShots);
        cleanedGame.HomeTakeawaysAvg = GetStatAvg(homeTeamSeasonStatGames, game.HomeTeamId, g => g.HomeTakeaways, g => g.AwayTakeaways);
        cleanedGame.AwayTakeawaysAvg = GetStatAvg(awayTeamSeasonStatGames, game.AwayTeamId, g => g.HomeTakeaways, g => g.AwayTakeaways);
        cleanedGame.HomeGiveawaysAvg = GetStatAvg(homeTeamSeasonStatGames, game.HomeTeamId, g => g.HomeGiveaways, g => g.AwayGiveaways);
        cleanedGame.AwayGiveawaysAvg = GetStatAvg(awayTeamSeasonStatGames, game.AwayTeamId, g => g.HomeGiveaways, g => g.AwayGiveaways);

        cleanedGame.HomeFaceOffWinPctAvg = GetDoubleStatAvg(homeTeamSeasonStatGames, game.HomeTeamId, g => g.HomeFaceOffWinPercent, g => g.AwayFaceOffWinPercent);
        cleanedGame.AwayFaceOffWinPctAvg = GetDoubleStatAvg(awayTeamSeasonStatGames, game.AwayTeamId, g => g.HomeFaceOffWinPercent, g => g.AwayFaceOffWinPercent);
        cleanedGame.HomeRecentFaceOffWinPctAvg = GetDoubleStatAvg(homeTeamRecentGames, game.HomeTeamId, g => g.HomeFaceOffWinPercent, g => g.AwayFaceOffWinPercent);
        cleanedGame.AwayRecentFaceOffWinPctAvg = GetDoubleStatAvg(awayTeamRecentGames, game.AwayTeamId, g => g.HomeFaceOffWinPercent, g => g.AwayFaceOffWinPercent);

        cleanedGame.HomeOvertimeRatio = GetOvertimeRatio(homeTeamSeasonStatGames);
        cleanedGame.AwayOvertimeRatio = GetOvertimeRatio(awayTeamSeasonStatGames);
        cleanedGame.HomeRecentOvertimeRatio = GetOvertimeRatio(homeTeamRecentGames);
        cleanedGame.AwayRecentOvertimeRatio = GetOvertimeRatio(awayTeamRecentGames);

        cleanedGame.HomeRegulationWinRatio = GetRegulationWinRatio(homeTeamSeasonStatGames, game.HomeTeamId);
        cleanedGame.AwayRegulationWinRatio = GetRegulationWinRatio(awayTeamSeasonStatGames, game.AwayTeamId);
        cleanedGame.HomeRecentRegulationWinRatio = GetRegulationWinRatio(homeTeamRecentGames, game.HomeTeamId);
        cleanedGame.AwayRecentRegulationWinRatio = GetRegulationWinRatio(awayTeamRecentGames, game.AwayTeamId);

        cleanedGame.HomeShootingPct = GetShootingPct(homeTeamSeasonStatGames, game.HomeTeamId);
        cleanedGame.AwayShootingPct = GetShootingPct(awayTeamSeasonStatGames, game.AwayTeamId);
        cleanedGame.HomeRecentShootingPct = GetShootingPct(homeTeamRecentGames, game.HomeTeamId);
        cleanedGame.AwayRecentShootingPct = GetShootingPct(awayTeamRecentGames, game.AwayTeamId);

        cleanedGame.HomeStrengthOfSchedule = GetStrengthOfSchedule(homeTeamSeasonStatGames, game.HomeTeamId, seasonGames);
        cleanedGame.AwayStrengthOfSchedule = GetStrengthOfSchedule(awayTeamSeasonStatGames, game.AwayTeamId, seasonGames);
        cleanedGame.HomeRecentStrengthOfSchedule = GetStrengthOfSchedule(homeTeamRecentGames, game.HomeTeamId, seasonGames);
        cleanedGame.AwayRecentStrengthOfSchedule = GetStrengthOfSchedule(awayTeamRecentGames, game.AwayTeamId, seasonGames);

        return cleanedGame;
    }

    /// <summary>
    /// Played games before the given date. Upcoming games are cleaned too, and unplayed games between now and
    /// then would otherwise count as 0-goal non-wins.
    /// </summary>
    private static IEnumerable<Game> PlayedBefore(this IEnumerable<Game> games, DateTime date) =>
        games.GetGamesBeforeDate(date).Where(g => g.HasBeenPlayed).ToList();

    /// <summary>
    /// The season's games (newest first), or the team's MIN_SEASON_GAMES most recent games when the season has fewer.
    /// </summary>
    private static IEnumerable<Game> TopUpSeasonGames(IEnumerable<Game> seasonGames, IEnumerable<Game> allGames) =>
        seasonGames.Count() >= MIN_SEASON_GAMES ? seasonGames : allGames.Take(MIN_SEASON_GAMES).ToList();

    public static double GetWinRatioOfGames(IEnumerable<Game> teamSeasonGames, int teamId)
    {
        double winRatio = 0;
        int count = 0;
        foreach (var game in teamSeasonGames)
        {
            if (game.IsWinner(teamId))
                winRatio++;
            count++;
        }
        if (count > 0)
            winRatio = winRatio / count;
        return winRatio;
    }

    public static double GetStatAvg(IEnumerable<Game> teamGames, int teamId,
        Func<Game, int> homeStat, Func<Game, int> awayStat)
    {
        double total = 0;
        int count = 0;
        foreach (var game in teamGames)
        {
            if (game.HomeTeamId == teamId)
                total += homeStat(game);
            else if (game.AwayTeamId == teamId)
                total += awayStat(game);

            count++;
        }
        if (count > 0)
            total = total / count;
        return total;
    }

    public static double GetSavePct(IEnumerable<Game> teamGames, int teamId)
    {
        int totalShotsAgainst = 0;
        int totalGoalsAgainst = 0;
        foreach (var game in teamGames)
        {
            if (game.HomeTeamId == teamId)
            {
                totalShotsAgainst += game.AwaySOG;
                totalGoalsAgainst += game.AwayGoals;
            }
            else if (game.AwayTeamId == teamId)
            {
                totalShotsAgainst += game.HomeSOG;
                totalGoalsAgainst += game.HomeGoals;
            }
        }
        if (totalShotsAgainst > 0)
            return (double)(totalShotsAgainst - totalGoalsAgainst) / totalShotsAgainst;
        return 0;
    }

    public static double GetGoalDiffAvg(IEnumerable<Game> teamGames, int teamId)
    {
        double total = 0;
        int count = 0;
        foreach (var game in teamGames)
        {
            if (game.HomeTeamId == teamId)
                total += game.HomeGoals - game.AwayGoals;
            else if (game.AwayTeamId == teamId)
                total += game.AwayGoals - game.HomeGoals;
            count++;
        }
        if (count > 0)
            total = total / count;
        return total;
    }

    public static double GetDoubleStatAvg(IEnumerable<Game> teamGames, int teamId,
        Func<Game, double> homeStat, Func<Game, double> awayStat)
    {
        double total = 0;
        int count = 0;
        foreach (var game in teamGames)
        {
            if (game.HomeTeamId == teamId)
                total += homeStat(game);
            else if (game.AwayTeamId == teamId)
                total += awayStat(game);
            count++;
        }
        if (count > 0)
            total = total / count;
        return total;
    }

    public static double GetOvertimeRatio(IEnumerable<Game> games)
    {
        int count = 0;
        int otCount = 0;
        foreach (var game in games)
        {
            if (game.EndPeriod != PeriodType.Regulation)
                otCount++;
            count++;
        }
        if (count > 0)
            return (double)otCount / count;
        return 0;
    }

    public static double GetRegulationWinRatio(IEnumerable<Game> teamGames, int teamId)
    {
        int count = 0;
        int regWins = 0;
        foreach (var game in teamGames)
        {
            if (game.IsWinner(teamId) && game.EndPeriod == PeriodType.Regulation)
                regWins++;
            count++;
        }
        if (count > 0)
            return (double)regWins / count;
        return 0;
    }

    public static double GetStreak(IEnumerable<Game> teamGames, int teamId)
    {
        int streak = 0;
        bool? streakIsWins = null;
        foreach (var game in teamGames)
        {
            bool won = game.IsWinner(teamId);
            if (streakIsWins == null)
            {
                streakIsWins = won;
                streak = won ? 1 : -1;
            }
            else if (won == streakIsWins)
            {
                streak += won ? 1 : -1;
            }
            else
            {
                break;
            }
        }
        return streak;
    }

    public static double GetShootingPct(IEnumerable<Game> teamGames, int teamId)
    {
        int totalGoals = 0;
        int totalSOG = 0;
        foreach (var game in teamGames)
        {
            if (game.HomeTeamId == teamId)
            {
                totalGoals += game.HomeGoals;
                totalSOG += game.HomeSOG;
            }
            else if (game.AwayTeamId == teamId)
            {
                totalGoals += game.AwayGoals;
                totalSOG += game.AwaySOG;
            }
        }
        if (totalSOG == 0)
            return 0;
        return (double)totalGoals / totalSOG;
    }

    public static double GetStrengthOfSchedule(IEnumerable<Game> teamGames, int teamId, SeasonGames seasonGames)
    {
        double totalOpponentWinPct = 0;
        int count = 0;
        foreach (var game in teamGames)
        {
            int opponentId = game.HomeTeamId == teamId ? game.AwayTeamId : game.HomeTeamId;

            if (!seasonGames.GamesMap.TryGetValue(opponentId, out var opponentGames))
                continue;

            var opponentPriorGames = opponentGames.CurrentSeasonGames.PlayedBefore(game.GameDateUTC);
            totalOpponentWinPct += GetWinRatioOfGames(opponentPriorGames, opponentId);
            count++;
        }
        if (count == 0)
            return 0;
        return totalOpponentWinPct / count;
    }
}