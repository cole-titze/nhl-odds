using Entities.Models;
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

        public async Task<IEnumerable<IGamePlayerStats>?> GetPlayerGameStats(Game game)
        {
            var gameId = game.id;
            var url = "http://api-web.nhle.com/v1/gamecenter/";
            var summaryQuery = GetGameQuery(gameId, GameRequestType.GameSummary);

            var gameSummaryResponse = await _requestMaker.MakeRequest(url, summaryQuery);
            if (gameSummaryResponse.response == null)
            {
                _logger.LogWarning("Failed to get game with id: " + gameId.ToString());
                return null;
            }

            if (IsGameDone(gameSummaryResponse.response.gameState))
            {
                return await GetPastGamePlayerStats(game);
            }

            return await GetCurrentGamePlayerStats(game);
        }

        private async Task<IEnumerable<IGamePlayerStats>> GetCurrentGamePlayerStats(Game game)
        {
            // Get current team roster to use
            var url = "https://api-web.nhle.com/v1/roster/";
            var homeQuery = game.homeTeamAbbr + "/current";
            var awayQuery = game.awayTeamAbbr + "/current";
            var homeRosterResponse = await _requestMaker.MakeRequest(url, homeQuery);
            var awayRosterResponse = await _requestMaker.MakeRequest(url, awayQuery);

            if (homeRosterResponse == null || awayRosterResponse == null)
            {
                _logger.LogWarning("Failed to get current roster for game with id: " + game.id.ToString());
                return new List<IGamePlayerStats>() { new GameSkaterStats() };
            }

            var homeRoster = homeRosterResponse.CurentRosterResponseToPlayerStats();
            var awayRoster = awayRosterResponse.CurentRosterResponseToPlayerStats();

            return homeRoster.Concat(awayRoster);
        }

        private async Task<IEnumerable<IGamePlayerStats>> GetPastGamePlayerStats(Game game)
        {
            var gameId = game.id;
            var url = "http://api-web.nhle.com/v1/gamecenter/";
            var statQuery = GetGameQuery(gameId, GameRequestType.GameStats);
            var gameStatResponse = await _requestMaker.MakeRequest(url, statQuery);
            if(gameStatResponse == null)
            {
                _logger.LogWarning("Failed to get game stats with id: " + gameId.ToString());
                return new List<IGamePlayerStats>() { new GameSkaterStats() };
            }

            return gameStatResponse.GameStatsResponseToPlayerStats();
        }

        public async Task<Player> GetPlayer(int playerId)
        {
            string url = "https://api-web.nhle.com/v1/player/";
            string summaryQuery = GetPlayerQuery(playerId);

            var playerSummaryResponse = await _requestMaker.MakeRequest(url, summaryQuery);

            if (playerSummaryResponse == null)
            {
                _logger.LogWarning("Failed to get player with id: " + playerId.ToString());
                return new Player();
            }

            return playerSummaryResponse.PlayerResponseToPlayer();
        }
    }
}

