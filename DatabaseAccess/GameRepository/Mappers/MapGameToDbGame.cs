using Entities.DbModels;
using Entities.Models;

namespace DataAccess.GameRepository.Mappers
{
    public static class MapGameToDbGame
    {
        public static DbGameRaw Map(Game game)
        {
            return new DbGameRaw()
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
            };
        }
        public static IEnumerable<DbGameRaw> Map(IEnumerable<Game> games)
		{
			var dbGames = new List<DbGameRaw>();
            foreach (var game in games)
            {
                dbGames.Add(Map(game));
            }
            return dbGames;
		}
	}
}

