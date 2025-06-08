using Entities.DbModels;
using Entities.Models;

namespace DataAccess.GameRepository.Mappers
{
    public static class MapDbGameToGame
    {
        public static Game Map(DbGameRaw game)
        {
            var extendedInfo = new GameExtendedInfo()
            {
                gameSummary = game.gameSummary,
                eventSummary = game.eventSummary,
                playByPlaySummary = game.playByPlaySummary,
                faceoffSummary = game.faceoffSummary,
                faceoffComparisonSummary = game.faceoffComparisonSummary,
                rosterSummary = game.rosterSummary,
                shotSummary = game.shotSummary,
                shiftChartSummary = game.shiftChartSummary,
                toiAwaySummary = game.toiAwaySummary,
                toiHomeSummary = game.toiHomeSummary,
                threeMinuteRecapVideoId = game.threeMinuteRecapVideoId,
                condensedGameVideoId = game.condensedGameVideoId,
            };
            return new Game()
            {
                id = game.id,
                homeTeamId = game.homeTeamId,
                awayTeamId = game.awayTeamId,
                seasonStartYear = game.seasonStartYear,
                gameDateUTC = game.gameDateUTC,
                homeGoals = game.homeGoals,
                awayGoals = game.awayGoals,
                winner = game.winner,
                homeSOG = game.homeSOG,
                awaySOG = game.awaySOG,
                homePPG = game.homePPG,
                awayPPG = game.awayPPG,
                homePIM = game.homePIM,
                awayPIM = game.awayPIM,
                homeFaceOffWinPercent = game.homeFaceOffWinPercent,
                awayFaceOffWinPercent = game.awayFaceOffWinPercent,
                homeBlockedShots = game.homeBlockedShots,
                awayBlockedShots = game.awayBlockedShots,
                homeHits = game.homeHits,
                awayHits = game.awayHits,
                homeTakeaways = game.homeTakeaways,
                awayTakeaways = game.awayTakeaways,
                homeGiveaways = game.homeGiveaways,
                awayGiveaways = game.awayGiveaways,
                hasBeenPlayed = game.hasBeenPlayed,
                extendedInfo = extendedInfo,
            };
        }
        public static IEnumerable<Game> Map(IEnumerable<DbGameRaw> games)
        {
            var gameList = new List<Game>();
            foreach (var game in games)
            {
                gameList.Add(Map(game));
            }

            return gameList;
        }
	}
}

