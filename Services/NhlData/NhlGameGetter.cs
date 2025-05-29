using Entities.DbModels;
using Microsoft.Extensions.Logging;
using Services.NhlData.Mappers;
using Services.RequestMaker;
using static Services.NhlData.NhlDataGetter;

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

        /// <summary>
        /// Calls the Nhl api and parses the response into a game.
        /// </summary>
        /// <param name="gameId">The game to get</param>
        /// <returns>A game object corresponding to the id passed in</returns>
        /// Example Request: https://api-web.nhle.com/v1/gamecenter/2023020204/boxscore
        public async Task<DbGameRaw> GetGame(int gameId)
        {
            string url = "http://api-web.nhle.com/v1/gamecenter/";
            string summaryQuery = GetGameQuery(gameId, GameRequestType.GameSummary);
            string statQuery = GetGameQuery(gameId, GameRequestType.GameStats);

            var gameSummaryResponse = await _requestMaker.MakeRequest(url, summaryQuery);
            var gameStatResponse = await _requestMaker.MakeRequest(url, statQuery);

            if (gameSummaryResponse == null || gameStatResponse == null)
            {
                _logger.LogWarning("Failed to get game with id: " + gameId.ToString());
                return new DbGameRaw();
            }
            if (NhlDataGetter.IsGameInProgress(gameSummaryResponse!.gameState))
                return new DbGameRaw();

            return MapGameResponseToGame.Map(gameSummaryResponse, gameStatResponse);
        }
    }
}

