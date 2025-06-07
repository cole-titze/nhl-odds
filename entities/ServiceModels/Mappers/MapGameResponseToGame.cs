using Entities.DbModels;
using Entities.Models;

namespace Entities.ServiceModels.Mappers
{
    public static class MapGameResponseToGame
    {
        /// <summary>
        /// Maps the response from the nhl's api to a game object
        /// </summary>
        /// <param name="message">Response from nhl api</param>
        /// <returns>Game Object</returns>
		public static Game Map(dynamic messageGameSummary, dynamic messageGamesStats)
        {
            var game = new Game();

            // Get game summary data
            game.homeTeamId = (int)messageGameSummary.homeTeam.id;
            game.awayTeamId = (int)messageGameSummary.awayTeam.id;
            game.id = (int)messageGameSummary.id;
            game.seasonStartYear = GetSeason((string)messageGameSummary.season);
            game.gameDateUTC = DateTime.Parse((string)messageGameSummary.startTimeUTC);
            game.hasBeenPlayed = (messageGameSummary.gameState == "OFF") ? true : false;

            // Get game stats data
            int homeGoals = (int)messageGamesStats.linescore.totals.home;
            int awayGoals = (int)messageGamesStats.linescore.totals.away;
            game.homeGoals = homeGoals;
            game.awayGoals = awayGoals;
            game.winner = GetWinner(homeGoals, awayGoals);
            foreach (dynamic statCategory in messageGamesStats.teamGameStats)
            {
                game = BuildGameStat(statCategory, game);
            }

            game.extendedInfo = GetGameExtendedInfo(messageGameSummary, messageGamesStats);

            return game;
        }
        /// <summary>
        /// Creates the extended info for the game.
        /// </summary>
        /// <param name="messageGameSummary">The game summary response</param>
        /// <param name="messageGamesStats">The game stats response</param>
        /// <returns>Extended infor about the game</returns>
        private static GameExtendedInfo GetGameExtendedInfo(dynamic messageGameSummary, dynamic messageGamesStats)
        {
            var tvBroadcasters = new List<TvBroadcaster>();
            foreach(var broadcaster in messageGameSummary.tvBroadcasts)
            {
                tvBroadcasters.Add(new TvBroadcaster()
                {
                    id = (int)broadcaster.id,
                    marketAbbreviation = (string)broadcaster.market,
                    networkName = (string)broadcaster.network,
                    countryCode = (string)broadcaster.countryCode,
                    sequenceNumber = (int)broadcaster.sequenceNumber,
                });
            }

            return new GameExtendedInfo()
            {
                tvBroadcasters = tvBroadcasters,
                venueName = (string)messageGameSummary.venue.@default,
                venueLocation = (string)messageGameSummary.venueLocation.@default,
            };
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
                    game.homeSOG = (int)statCategory.homeValue;
                    game.awaySOG = (int)statCategory.awayValue;
                    break;
                case "faceoffWinningPctg":
                    game.homeFaceOffWinPercent = (double)statCategory.homeValue;
                    game.awayFaceOffWinPercent = (double)statCategory.awayValue;
                    break;
                case "pim":
                    game.homePIM = (int)statCategory.homeValue;
                    game.awayPIM = (int)statCategory.awayValue;
                    break;
                case "hits":
                    game.homeHits = (int)statCategory.homeValue;
                    game.awayHits = (int)statCategory.awayValue;
                    break;
                case "blockedShots":
                    game.homeBlockedShots = (int)statCategory.homeValue;
                    game.awayBlockedShots = (int)statCategory.awayValue;
                    break;
                case "powerPlay":
                    string homePowerPlayConversionStr = (string)statCategory.homeValue;
                    string awayPowerPlayConversionStr = (string)statCategory.awayValue;

                    game.homePPG = int.Parse(new string(homePowerPlayConversionStr.TakeWhile(Char.IsDigit).ToArray()));
                    game.awayPPG = int.Parse(new string(awayPowerPlayConversionStr.TakeWhile(Char.IsDigit).ToArray()));
                    break;
                case "giveaways":
                    game.homeGiveaways = (int)statCategory.homeValue;
                    game.awayGiveaways = (int)statCategory.awayValue;
                    break;
                case "takeaways":
                    game.homeTakeaways = (int)statCategory.homeValue;
                    game.awayTakeaways = (int)statCategory.awayValue;
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
}
