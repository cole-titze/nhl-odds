using Entities.Models;
using Entities.Types;
using Entities.Types.Enums;

namespace Entities.ServiceModels.Mappers;

public static class MapGameResponseToGame
{
    /// <summary>
    /// Maps the response from the nhl's api to a game object
    /// </summary>
    /// <param name="message">Response from nhl api</param>
    /// <returns>Game Object</returns>
    public static Game Map(dynamic messageGameSummary, dynamic messageGamesStats, dynamic messageGameEvents)
    {
        var game = new Game();

        // Get game summary data
        game.HomeTeamId = (int)messageGameSummary.homeTeam.id;
        game.AwayTeamId = (int)messageGameSummary.awayTeam.id;
        game.HomeTeamAbbr = (string)messageGameSummary.homeTeam.abbrev;
        game.AwayTeamAbbr = (string)messageGameSummary.awayTeam.abbrev;
        game.Id = (int)messageGameSummary.id;
        game.SeasonStartYear = GetSeason((string)messageGameSummary.season);
        game.GameDateUTC = DateTime.Parse((string)messageGameSummary.startTimeUTC);
        game.HasBeenPlayed = (messageGameSummary.gameState == "OFF") ? true : false;

        if (game.HasBeenPlayed)
        {
            // Get game stats data
            int homeGoals = (int)messageGamesStats.linescore.totals.home;
            int awayGoals = (int)messageGamesStats.linescore.totals.away;
            game.HomeGoals = homeGoals;
            game.AwayGoals = awayGoals;
            game.Winner = GetWinner(homeGoals, awayGoals);
            game.EndPeriod = PeriodTypeParser.ParseFromString((string)messageGameSummary.periodDescriptor.periodType);
            foreach (dynamic statCategory in messageGamesStats.teamGameStats)
            {
                game = BuildGameStat(statCategory, game);
            }

            game.ExtendedInfo = MapGameSummaryToGameExtendedInfo.Map(messageGameSummary);
            game.GameEvents = MapGameEventsResponseToGameEvents.Map(messageGameEvents);
        }

        return game;
    }

    /// <summary>
    /// Given a stat category from the API response, updates the game object with the relevant stats.
    /// </summary>
    /// <param name="statCategory">The category from the API response</param>
    /// <param name="game">The game object to build</param>
    /// <returns>game</returns>
    private static Game BuildGameStat(dynamic statCategory, Game game)
    {
        string categoryName = (string)statCategory.category;
        switch (categoryName)
        {
            case "sog":
                game.HomeSOG = (int)statCategory.homeValue;
                game.AwaySOG = (int)statCategory.awayValue;
                break;
            case "faceoffWinningPctg":
                game.HomeFaceOffWinPercent = (double)statCategory.homeValue;
                game.AwayFaceOffWinPercent = (double)statCategory.awayValue;
                break;
            case "pim":
                game.HomePIM = (int)statCategory.homeValue;
                game.AwayPIM = (int)statCategory.awayValue;
                break;
            case "hits":
                game.HomeHits = (int)statCategory.homeValue;
                game.AwayHits = (int)statCategory.awayValue;
                break;
            case "blockedShots":
                game.HomeBlockedShots = (int)statCategory.homeValue;
                game.AwayBlockedShots = (int)statCategory.awayValue;
                break;
            case "powerPlay":
                string homePowerPlayConversionStr = (string)statCategory.homeValue;
                string awayPowerPlayConversionStr = (string)statCategory.awayValue;

                game.HomePPG = int.Parse(new string(homePowerPlayConversionStr.TakeWhile(Char.IsDigit).ToArray()));
                game.AwayPPG = int.Parse(new string(awayPowerPlayConversionStr.TakeWhile(Char.IsDigit).ToArray()));
                break;
            case "giveaways":
                game.HomeGiveaways = (int)statCategory.homeValue;
                game.AwayGiveaways = (int)statCategory.awayValue;
                break;
            case "takeaways":
                game.HomeTakeaways = (int)statCategory.homeValue;
                game.AwayTakeaways = (int)statCategory.awayValue;
                break;

            default:
                break;
        }

        return game;
    }

    /// <summary>
    /// Determines who won the game.
    /// </summary>
    /// <param name="homeGoals">Home team goals</param>
    /// <param name="awayGoals">Away team goals</param>
    /// <returns>Winner.Home if home won and Winner.Away if away won</returns>
    private static Winner GetWinner(int homeGoals, int awayGoals)
    {
        if (homeGoals > awayGoals)
            return Winner.HOME;
        return Winner.AWAY;
    }
    /// <summary>
    /// Gets the season start year from season string
    /// </summary>
    /// <param name="season">Season string (ex. 20212022)</param>
    /// <returns>Season start year</returns>
    private static int GetSeason(string season)
    {
        var yearStr = season.Substring(0, 4);
        return int.Parse(yearStr);
    }
}
