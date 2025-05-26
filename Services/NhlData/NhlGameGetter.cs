using Entities.DbModels;
using Microsoft.Extensions.Logging;
using Services.NhlData.Mappers;
using Services.RequestMaker;

namespace Services.NhlData
{
    public class NhlGameGetter : INhlGameGetter
	{
        private readonly IRequestMaker _requestMaker;
        private readonly ILogger<NhlGameGetter> _logger;

        public NhlGameGetter(IRequestMaker requestMaker, ILoggerFactory loggerFactory)
        {
            _requestMaker = requestMaker;
            _logger = loggerFactory.CreateLogger<NhlGameGetter>();
        }

        private enum GameRequestType
        {
            /// <summary>
            /// Request type for game summary. This holds data like the team ID's and status
            /// </summary>
            GameSummary,
            /// <summary>
            /// Request type for game stats. This holds data like goals, shots on goal, and other stats
            /// </summary>
            GameStats
        }
        /// <summary>
        /// Calls the Nhl api and parses the response into a game.
        /// </summary>
        /// <param name="gameId">The game to get</param>
        /// <returns>A game object corresponding to the id passed in</returns>
        /// Example Request: https://api-web.nhle.com/v1/gamecenter/2023020204/boxscore
        public async Task<DbGame> GetGame(int gameId)
        {
            string url = "http://api-web.nhle.com/v1/gamecenter/";
            string summaryQuery = GetGameQuery(gameId, GameRequestType.GameSummary);
            string statQuery = GetGameQuery(gameId, GameRequestType.GameStats);

            var gameSummaryResponse = await _requestMaker.MakeRequest(url, summaryQuery);
            var gameStatResponse = await _requestMaker.MakeRequest(url, statQuery);

            if (gameSummaryResponse == null || gameStatResponse == null)
            {
                _logger.LogWarning("Failed to get game with id: " + gameId.ToString());
                return new DbGame();
            }
            if (IsGameInProgress(gameSummaryResponse))
                return new DbGame();

            return MapGameResponseToGame.Map(gameSummaryResponse, gameStatResponse);
        }
        /// <summary>
        /// Determines if a game is in progress or not
        /// </summary>
        /// <param name="message">response from nhl api</param>
        /// <returns>True if game is in progress, otherwise false</returns>
        private static bool IsGameInProgress(dynamic message)
        {
            if (message.gameState != "OFF" && message.gameState != "FUT")
                return true;

            return false;
        }
        /// <summary>
        /// Creates the game query
        /// </summary>
        /// <param name="id"></param>
        /// <param name="requestType">The type of request to make</param>
        /// <returns>Game query string</returns>
        private static string GetGameQuery(int id, GameRequestType requestType)
        {
            string urlParameters = string.Empty;
            switch (requestType)
            {
                case GameRequestType.GameSummary:
                    urlParameters = $"{id}/boxscore";
                    break;
                case GameRequestType.GameStats:
                    urlParameters = $"{id}/right-rail";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(requestType), requestType, null);
            }
            
            return urlParameters;
        }
    }
}

