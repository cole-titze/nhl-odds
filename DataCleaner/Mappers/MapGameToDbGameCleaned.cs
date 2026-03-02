using Entities.DbModels;
using Entities.Models;

namespace DataCleaner.Mappers;

public static class MapGameToDbGameCleaned
{
    private const int RECENT_GAMES = 5;

    public static DbGameCleaned Map(Game game, SeasonGames seasonGames, RosterScorer? rosterScorer = null)
    {
        var homeTeamGames = seasonGames.GamesMap[game.HomeTeamId];
        var awayTeamGames = seasonGames.GamesMap[game.AwayTeamId];
        // Lists of team games for current season
        var homeTeamSeasonGames = homeTeamGames.CurrentSeasonGames.GetGamesBeforeDate(game.GameDateUTC);
        var awayTeamSeasonGames = awayTeamGames.CurrentSeasonGames.GetGamesBeforeDate(game.GameDateUTC);
        // List of recently played team games
        var homeTeamRecentGames = homeTeamGames.Games.GetGamesBeforeDate(game.GameDateUTC).Take(RECENT_GAMES);
        var awayTeamRecentGames = awayTeamGames.Games.GetGamesBeforeDate(game.GameDateUTC).Take(RECENT_GAMES);
        // List of team games played that match current home/away position
        var homeTeamHomeGames = homeTeamGames.HomeGames.GetGamesBeforeDate(game.GameDateUTC);
        var awayTeamAwayGames = awayTeamGames.AwayGames.GetGamesBeforeDate(game.GameDateUTC);
        // List of recent team games played that match current home/away position
        var homeTeamRecentHomeGames = homeTeamGames.HomeGames.GetGamesBeforeDate(game.GameDateUTC).Take(RECENT_GAMES);
        var awayTeamRecentAwayGames = awayTeamGames.AwayGames.GetGamesBeforeDate(game.GameDateUTC).Take(RECENT_GAMES);

        var homeRoster = rosterScorer?.GetTeamRosterValues(game.Id, game.HomeTeamId);
        var awayRoster = rosterScorer?.GetTeamRosterValues(game.Id, game.AwayTeamId);

        var cleanedGame = new DbGameCleaned()
        {
            GameId = game.Id,

            HomeWinRatio = GetWinRatioOfGames(homeTeamSeasonGames, game.HomeTeamId),
            HomeRecentWinRatio = GetWinRatioOfGames(homeTeamRecentGames, game.HomeTeamId),
            HomeGoalsAvg = GetStatAvg(homeTeamSeasonGames, game.HomeTeamId, g => g.HomeGoals, g => g.AwayGoals),
            HomeRecentGoalsAvg = GetStatAvg(homeTeamRecentGames, game.HomeTeamId, g => g.HomeGoals, g => g.AwayGoals),
            HomeConcededGoalsAvg = GetStatAvg(homeTeamSeasonGames, game.HomeTeamId, g => g.AwayGoals, g => g.HomeGoals),
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
            HomeHoursSinceLastGame = game.GetHoursBetweenGames(homeTeamSeasonGames.FirstOrDefault()),
            HomeRosterOffenseValue = homeRoster?.RosterOffenseValue ?? 0,
            HomeRosterDefenseValue = homeRoster?.RosterDefenseValue ?? 0,
            HomeRosterGoalieValue = homeRoster?.RosterGoalieValue ?? 0,
            HomeRecentRosterOffenseValue = homeRoster?.RecentRosterOffenseValue ?? 0,
            HomeRecentRosterDefenseValue = homeRoster?.RecentRosterDefenseValue ?? 0,
            HomeRecentRosterGoalieValue = homeRoster?.RecentRosterGoalieValue ?? 0,

            AwayWinRatio = GetWinRatioOfGames(awayTeamSeasonGames, game.AwayTeamId),
            AwayRecentWinRatio = GetWinRatioOfGames(awayTeamRecentGames, game.AwayTeamId),
            AwayGoalsAvg = GetStatAvg(awayTeamSeasonGames, game.AwayTeamId, g => g.HomeGoals, g => g.AwayGoals),
            AwayRecentGoalsAvg = GetStatAvg(awayTeamRecentGames, game.AwayTeamId, g => g.HomeGoals, g => g.AwayGoals),
            AwayConcededGoalsAvg = GetStatAvg(awayTeamSeasonGames, game.AwayTeamId, g => g.AwayGoals, g => g.HomeGoals),
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
            AwayHoursSinceLastGame = game.GetHoursBetweenGames(awayTeamSeasonGames.FirstOrDefault()),
            AwayRosterOffenseValue = awayRoster?.RosterOffenseValue ?? 0,
            AwayRosterDefenseValue = awayRoster?.RosterDefenseValue ?? 0,
            AwayRosterGoalieValue = awayRoster?.RosterGoalieValue ?? 0,
            AwayRecentRosterOffenseValue = awayRoster?.RecentRosterOffenseValue ?? 0,
            AwayRecentRosterDefenseValue = awayRoster?.RecentRosterDefenseValue ?? 0,
            AwayRecentRosterGoalieValue = awayRoster?.RecentRosterGoalieValue ?? 0,
        };
        return cleanedGame;
    }

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
}
