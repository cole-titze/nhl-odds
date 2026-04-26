using Entities.Models;
using Entities.ServiceModels;

using Microsoft.Extensions.Logging;

using Services.RequestMaker;

using static Services.NhlData.NhlApiDataGetter;

namespace Services.NhlData;

public class NhlApiPlayerGetter : INhlPlayerGetter
{
    private readonly IRequestMaker _requestMaker;
    private readonly ILogger<NhlApiPlayerGetter> _logger;

    public NhlApiPlayerGetter(IRequestMaker requestMaker, ILoggerFactory loggerFactory)
    {
        _requestMaker = requestMaker;
        _logger = loggerFactory.CreateLogger<NhlApiPlayerGetter>();
    }
    /// <summary>
    /// Gets the player stats for a game. This will return the players that are on the roster 
    /// for a future game or the players that played in a past game. First calls the game summary 
    /// to determine if the game is done or not. Example call:
    /// https://api-web.nhle.com/v1/gamecenter/2024020325/boxscore
    /// </summary>
    /// <param name="game">The game to get the player stats for</param>
    /// <returns>The player stats for a game</returns>
    public async Task<GameRosterStats?> BuildGameRosterStats(Game game)
    {
        var gameId = game.Id;
        var url = "https://api-web.nhle.com/v1/gamecenter/";
        var summaryQuery = GetGameQuery(gameId, GameRequestType.GameSummary);

        var gameSummaryResponse = new ServiceGameSummaryResponse(await _requestMaker.MakeRequest(url, summaryQuery));
        if (gameSummaryResponse.response == null)
        {
            _logger.LogWarning("Failed to get game with id: " + gameId.ToString());
            return null;
        }

        if (gameSummaryResponse.IsGameDone())
        {
            return await BuildPastGameRosterStats(game, gameSummaryResponse);
        }

        return await BuildFutureGameRosterStats(game);
    }
    /// <summary>
    /// Gets the players that are on the roster for a future game. Example call:
    /// https://api-web.nhle.com/v1/roster/TOR/current
    /// </summary>
    /// <param name="game">The game to get the roster for</param>
    /// <returns>The gamePlayerStats of the two teams</returns>
    private async Task<GameRosterStats?> BuildFutureGameRosterStats(Game game)
    {
        // Get current team roster to use
        var url = "https://api-web.nhle.com/v1/roster/";
        var homeQuery = game.HomeTeamAbbr + "/current";
        var awayQuery = game.AwayTeamAbbr + "/current";
        var homeResponse = await _requestMaker.MakeRequest(url, homeQuery);
        var awayResponse = await _requestMaker.MakeRequest(url, awayQuery);

        if (homeResponse == null || awayResponse == null)
        {
            _logger.LogWarning("Failed to get current roster for game with id: " + game.Id.ToString());
            return null;
        }
        var rosterResponse = new ServiceRosterResponse(homeResponse, awayResponse);

        var gameRoster = rosterResponse.CurentRosterResponseToGameRosterStats(game.HomeTeamId, game.AwayTeamId);
        game.RosterStats = gameRoster;

        return gameRoster;
    }
    /// <summary>
    /// Gets the player stats for a past game. Example call:
    /// https://api-web.nhle.com/v1/gamecenter/2024020325/right-rail
    /// </summary>
    /// <param name="game">The game to get the roster for</param>
    /// <returns>Collection of GamePlayerStats</returns>
    private async Task<GameRosterStats?> BuildPastGameRosterStats(Game game, ServiceGameSummaryResponse gameSummaryResonse)
    {
        var gameId = game.Id;
        var url = "https://api-web.nhle.com/v1/gamecenter/";
        var statQuery = GetGameQuery(gameId, GameRequestType.GameStats);
        var gameStatResponse = new ServiceGameStatResponse(await _requestMaker.MakeRequest(url, statQuery));
        if (gameStatResponse.response == null)
        {
            _logger.LogWarning("Failed to get game stats with id: " + gameId.ToString());
            return null;
        }

        var gameRoster = gameStatResponse.GameStatsResponseToGameRosterStats(gameSummaryResonse);
        game.RosterStats = gameRoster;

        return gameRoster;
    }

    /// <summary>
    /// Gets the a player by their id. Example call:
    /// https://api-web.nhle.com/v1/player/8478402/landing
    /// </summary>
    /// <param name="playerId">Id of the player</param>
    /// <param name="currentGameTeamId">The team id of the most recent game the player played in</param>
    /// <returns>Player object</returns>
    public async Task<Player?> GetPlayer(int playerId, int currentGameTeamId)
    {
        string url = "https://api-web.nhle.com/v1/player/";
        string summaryQuery = GetPlayerQuery(playerId);

        var playerSummaryResponse = new ServicePlayerResponse(await _requestMaker.MakeRequest(url, summaryQuery));

        if (playerSummaryResponse.response == null)
        {
            _logger.LogWarning("Failed to get player with id: " + playerId.ToString());
            return null;
        }

        return playerSummaryResponse.PlayerResponseToPlayer(currentGameTeamId);
    }
}