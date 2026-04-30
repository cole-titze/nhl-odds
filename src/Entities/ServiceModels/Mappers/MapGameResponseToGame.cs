using System.Text.Json.Nodes;
using Entities.Models;
using Entities.Types;
using Entities.Types.Enums;

namespace Entities.ServiceModels.Mappers;

public static class MapGameResponseToGame
{
    public static Game Map(JsonNode? messageGameSummary, JsonNode? messageGamesStats, JsonNode? messageGameEvents)
    {
        var game = new Game();

        game.HomeTeamId = messageGameSummary!["homeTeam"]!["id"]!.GetValue<int>();
        game.AwayTeamId = messageGameSummary["awayTeam"]!["id"]!.GetValue<int>();
        game.HomeTeamAbbr = messageGameSummary["homeTeam"]!["abbrev"]!.GetValue<string>();
        game.AwayTeamAbbr = messageGameSummary["awayTeam"]!["abbrev"]!.GetValue<string>();
        game.Id = messageGameSummary["id"]!.GetValue<int>();
        game.GameType = ((game.Id % 1_000_000) / 10_000) == 3 ? GameType.Playoff : GameType.Regular;
        game.SeasonStartYear = messageGameSummary["season"]!.GetCoercedInt() / 10000;
        game.GameDateUTC = DateTime.Parse(messageGameSummary["startTimeUTC"]!.GetValue<string>());
        game.HasBeenPlayed = messageGameSummary["gameState"]!.GetValue<string>() == "OFF";

        if (game.HasBeenPlayed)
        {
            int homeGoals = messageGamesStats!["linescore"]!["totals"]!["home"]!.GetValue<int>();
            int awayGoals = messageGamesStats["linescore"]!["totals"]!["away"]!.GetValue<int>();
            game.HomeGoals = homeGoals;
            game.AwayGoals = awayGoals;
            game.Winner = GetWinner(homeGoals, awayGoals);
            game.EndPeriod = PeriodTypeParser.ParseFromString(messageGameSummary["periodDescriptor"]!["periodType"]!.GetValue<string>());
            foreach (var statCategory in messageGamesStats["teamGameStats"]!.AsArray())
            {
                game = BuildGameStat(statCategory!, game);
            }

            game.ExtendedInfo = MapGameSummaryToGameExtendedInfo.Map(messageGameSummary);
            game.GameEvents = MapGameEventsResponseToGameEvents.Map(messageGameEvents);
        }

        return game;
    }

    private static Game BuildGameStat(JsonNode statCategory, Game game)
    {
        string categoryName = statCategory["category"]!.GetValue<string>();
        switch (categoryName)
        {
            case "sog":
                game.HomeSOG = statCategory["homeValue"]!.GetValue<int>();
                game.AwaySOG = statCategory["awayValue"]!.GetValue<int>();
                break;
            case "faceoffWinningPctg":
                game.HomeFaceOffWinPercent = statCategory["homeValue"]!.GetValue<double>();
                game.AwayFaceOffWinPercent = statCategory["awayValue"]!.GetValue<double>();
                break;
            case "pim":
                game.HomePIM = statCategory["homeValue"]!.GetValue<int>();
                game.AwayPIM = statCategory["awayValue"]!.GetValue<int>();
                break;
            case "hits":
                game.HomeHits = statCategory["homeValue"]!.GetValue<int>();
                game.AwayHits = statCategory["awayValue"]!.GetValue<int>();
                break;
            case "blockedShots":
                game.HomeBlockedShots = statCategory["homeValue"]!.GetValue<int>();
                game.AwayBlockedShots = statCategory["awayValue"]!.GetValue<int>();
                break;
            case "powerPlay":
                string homePowerPlayConversionStr = statCategory["homeValue"]!.GetValue<string>();
                string awayPowerPlayConversionStr = statCategory["awayValue"]!.GetValue<string>();

                game.HomePPG = int.Parse(new string(homePowerPlayConversionStr.TakeWhile(Char.IsDigit).ToArray()));
                game.AwayPPG = int.Parse(new string(awayPowerPlayConversionStr.TakeWhile(Char.IsDigit).ToArray()));
                break;
            case "giveaways":
                game.HomeGiveaways = statCategory["homeValue"]!.GetValue<int>();
                game.AwayGiveaways = statCategory["awayValue"]!.GetValue<int>();
                break;
            case "takeaways":
                game.HomeTakeaways = statCategory["homeValue"]!.GetValue<int>();
                game.AwayTakeaways = statCategory["awayValue"]!.GetValue<int>();
                break;

            default:
                break;
        }

        return game;
    }

    private static Winner GetWinner(int homeGoals, int awayGoals)
    {
        if (homeGoals > awayGoals)
            return Winner.HOME;
        return Winner.AWAY;
    }

}