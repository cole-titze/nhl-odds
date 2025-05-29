using Entities.DbModels;
using Microsoft.Extensions.Logging;
using Services.NhlData.Mappers;
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

        public async Task<IEnumerable<IDbGamePlayerStats>> GetPlayerGameStats(DbGameRaw game)
        {
            var gameId = game.id;
            var url = "http://api-web.nhle.com/v1/gamecenter/";
            var summaryQuery = GetGameQuery(gameId, GameRequestType.GameSummary);

            var gameSummaryResponse = await _requestMaker.MakeRequest(url, summaryQuery);
            if (gameSummaryResponse == null)
            {
                _logger.LogWarning("Failed to get game with id: " + gameId.ToString());
                return new List<IDbGamePlayerStats>() { new DbGameSkaterStats() };
            }

            if (IsGameDone(gameSummaryResponse))
            {
                return await GetPastGamePlayers(game);
            }

            return await GetCurrentGamePlayers(game);
        }

        private async Task<IEnumerable<IDbGamePlayerStats>> GetCurrentGamePlayers(DbGameRaw game)
        {
            // Get current team roster to use
            var url = "https://api-web.nhle.com/v1/roster/";
            var homeQuery = game.homeTeam.abbreviation + "/current";
            var awayQuery = game.awayTeam.abbreviation + "/current";
            var homeRosterResponse = await _requestMaker.MakeRequest(url, homeQuery);
            var awayRosterResponse = await _requestMaker.MakeRequest(url, awayQuery);

            if (homeRosterResponse == null || awayRosterResponse == null)
            {
                _logger.LogWarning("Failed to get current roster for game with id: " + game.id.ToString());
                return new List<IDbGamePlayerStats>() { new DbGameSkaterStats() };
            }

            var homeRoster = MapCurrentRosterResponseToGamePlayerStats.Map(homeRosterResponse);
            var awayRoster = MapCurrentRosterResponseToGamePlayerStats.Map(awayRosterResponse);

            return homeRoster.Concat(awayRoster);
        }

        private async Task<IEnumerable<IDbGamePlayerStats>> GetPastGamePlayers(DbGameRaw game)
        {
            var gameId = game.id;
            var url = "http://api-web.nhle.com/v1/gamecenter/";
            var statQuery = GetGameQuery(gameId, GameRequestType.GameStats);
            var gameStatResponse = await _requestMaker.MakeRequest(url, statQuery);
            if(gameStatResponse == null)
            {
                _logger.LogWarning("Failed to get game stats with id: " + gameId.ToString());
                return new List<IDbGamePlayerStats>() { new DbGameSkaterStats() };
            }

            return MapGamePlayerStatsResponseToGamePlayerStats.Map(gameStatResponse);
        }

        public async Task<DbPlayer> GetPlayer(int playerId)
        {
            string url = "https://api-web.nhle.com/v1/player/";
            string summaryQuery = GetPlayerQuery(playerId);

            var playerSummaryResponse = await _requestMaker.MakeRequest(url, summaryQuery);

            if (playerSummaryResponse == null)
            {
                _logger.LogWarning("Failed to get player with id: " + playerId.ToString());
                return new DbPlayer();
            }

            return MapPlayerResponseToPlayer.Map(playerSummaryResponse);
        }
    }
}

