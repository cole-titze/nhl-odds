using Entities.Models;
using Entities.ServiceModels;
using Microsoft.Extensions.Logging;
using Services.RequestMaker;
using static Services.NhlData.NhlDataGetter;

namespace Services.NhlData
{
    public class NhlPlayerGetter : INhlPlayerGetter
    {
        private readonly IRequestMaker _requestMaker;
        private readonly ILogger<NhlPlayerGetter> _logger;
        private const int DEFAULT_GAME_COUNT = 1400;

        public NhlPlayerGetter(IRequestMaker requestMaker, ILoggerFactory loggerFactory)
        {
            _requestMaker = requestMaker;
            _logger = loggerFactory.CreateLogger<NhlPlayerGetter>();
        }
        /// <summary>
        /// Gets the player stats for a game. This will return the players that are on the roster 
        /// for a future game or the players that played in a past game. First calls the game summary 
        /// to determine if the game is done or not. Example call:
        /// https://api-web.nhle.com/v1/gamecenter/2024020325/boxscore
        /// </summary>
        /// <param name="game">The game to get the player stats for</param>
        /// <returns>The player stats for a game</returns>
        public async Task<IEnumerable<IGamePlayerStats>?> GetPlayerGameStats(Game game)
        {
            var gameId = game.id;
            var url = "http://api-web.nhle.com/v1/gamecenter/";
            var summaryQuery = GetGameQuery(gameId, GameRequestType.GameSummary);

            var gameSummaryResponse = new ServiceGameSummaryResponse(await _requestMaker.MakeRequest(url, summaryQuery));
            if (gameSummaryResponse.response == null)
            {
                _logger.LogWarning("Failed to get game with id: " + gameId.ToString());
                return null;
            }

            if (gameSummaryResponse.IsGameDone())
            {
                return await GetPastGamePlayerStats(game);
            }

            return await GetFutureGamePlayerStats(game);
        }
        /// <summary>
        /// Gets the players that are on the roster for a future game. Example call:
        /// https://api-web.nhle.com/v1/roster/TOR/current
        /// </summary>
        /// <param name="game">The game to get the roster for</param>
        /// <returns>The gamePlayerStats of the two teams</returns>
        private async Task<IEnumerable<IGamePlayerStats>> GetFutureGamePlayerStats(Game game)
        {
            // Get current team roster to use
            var url = "https://api-web.nhle.com/v1/roster/";
            var homeQuery = game.homeTeamAbbr + "/current";
            var awayQuery = game.awayTeamAbbr + "/current";
            var homeRosterResponse = new ServiceRosterResponse(await _requestMaker.MakeRequest(url, homeQuery));
            var awayRosterResponse = new ServiceRosterResponse(await _requestMaker.MakeRequest(url, awayQuery));

            if (homeRosterResponse.response == null || awayRosterResponse.response == null)
            {
                _logger.LogWarning("Failed to get current roster for game with id: " + game.id.ToString());
                return new List<IGamePlayerStats>() { new GameSkaterStats() };
            }

            var homeRoster = homeRosterResponse.CurentRosterResponseToPlayerStats(game.id, game.homeTeamId);
            var awayRoster = awayRosterResponse.CurentRosterResponseToPlayerStats(game.id, game.awayTeamId);

            return homeRoster.Concat(awayRoster);
        }
        /// <summary>
        /// Gets the player stats for a past game. Example call:
        /// https://api-web.nhle.com/v1/gamecenter/2024020325/right-rail
        /// </summary>
        /// <param name="game">The game to get the roster for</param>
        /// <returns>Collection of GamePlayerStats</returns>
        private async Task<IEnumerable<IGamePlayerStats>> GetPastGamePlayerStats(Game game)
        {
            var gameId = game.id;
            var url = "http://api-web.nhle.com/v1/gamecenter/";
            var statQuery = GetGameQuery(gameId, GameRequestType.GameStats);
            var gameStatResponse = new ServiceGameStatResponse(await _requestMaker.MakeRequest(url, statQuery));
            if(gameStatResponse.response == null)
            {
                _logger.LogWarning("Failed to get game stats with id: " + gameId.ToString());
                return new List<IGamePlayerStats>() { new GameSkaterStats() };
            }

            return gameStatResponse.GameStatsResponseToPlayerStats();
        }

        /// <summary>
        /// Gets the a player by their id. Example call:
        /// https://api-web.nhle.com/v1/player/8478402/landing
        /// </summary>
        /// <param name="playerId">Id of the player</param>
        /// <returns>Player object</returns>
        public async Task<Player> GetPlayer(int playerId)
        {
            string url = "https://api-web.nhle.com/v1/player/";
            string summaryQuery = GetPlayerQuery(playerId);

            var playerSummaryResponse = new ServicePlayerResponse(await _requestMaker.MakeRequest(url, summaryQuery));

            if (playerSummaryResponse.response == null)
            {
                _logger.LogWarning("Failed to get player with id: " + playerId.ToString());
                return new Player();
            }

            return playerSummaryResponse.PlayerResponseToPlayer();
        }
    }
}

