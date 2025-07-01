using Entities.Models;
using Entities.ServiceModels;
using Microsoft.Extensions.Logging;
using Services.RequestMaker;
using static Services.NhlData.NhlDataGetter;

namespace Services.NhlData;

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
    /// Example Requests: 
    /// https://api-web.nhle.com/v1/gamecenter/2023020204/boxscore
    /// https://api-web.nhle.com/v1/gamecenter/2023020204/right-rail
    /// https://api-web.nhle.com/v1/gamecenter/2024020279/play-by-play
    /// </summary>
    /// <param name="gameId">The game to get</param>
    /// <returns>A game object corresponding to the id passed in</returns>
    public async Task<Game?> GetGame(int gameId)
    {
        string url = "http://api-web.nhle.com/v1/gamecenter/";
        string summaryQuery = GetGameQuery(gameId, GameRequestType.GameSummary);
        string statQuery = GetGameQuery(gameId, GameRequestType.GameStats);
        string playByPlayQuery = GetGameQuery(gameId, GameRequestType.GameEvents);

        var gameSummaryResponse = new ServiceGameSummaryResponse(await _requestMaker.MakeRequest(url, summaryQuery));
        var gameStatResponse = new ServiceGameStatResponse(await _requestMaker.MakeRequest(url, statQuery));
        var gameEventResponse = new ServiceGameEventResponse(await _requestMaker.MakeRequest(url, playByPlayQuery));

        if (gameSummaryResponse.response == null || gameStatResponse.response == null || gameEventResponse.response == null)
        {
            _logger.LogWarning("Failed to get game with id: " + gameId.ToString());
            return null;
        }
        if (gameSummaryResponse.IsGameInProgress())
        {
            _logger.LogWarning("Game with id: " + gameId.ToString() + " is in progress, cannot get game data.");
            return null;
        }

        return gameSummaryResponse.GameSummaryResponseToGame(gameStatResponse, gameEventResponse);
    }
}

