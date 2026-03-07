using DatabaseAccess.WebTeamRepository.Mappers;
using Entities.DbModels;
using Entities.Models.Web;
using Entities.Types;

namespace DatabaseAccess.WebGameOddsRepository.Mappers;

public static class DbGameOddsToGameOddsMapper
{
    public static IEnumerable<GameOdds> Map(IEnumerable<DbGameOdds> dbGameOdds, Dictionary<int, DbSeasonTeam> seasonTeams)
    {
        var gameOddsList = new List<GameOdds>();
        foreach (var dbOdds in dbGameOdds)
        {
            if (dbOdds.Game == null)
                continue;

            var game = dbOdds.Game;
            seasonTeams.TryGetValue(game.HomeTeamId, out var homeSeasonTeam);
            seasonTeams.TryGetValue(game.AwayTeamId, out var awaySeasonTeam);

            gameOddsList.Add(new GameOdds
            {
                game = new Game
                {
                    id = game.Id,
                    gameDate = game.GameDateUTC,
                    homeGoals = game.HomeGoals,
                    awayGoals = game.AwayGoals,
                    seasonStartYear = game.SeasonStartYear,
                    winner = game.Winner,
                    hasBeenPlayed = game.HasBeenPlayed,
                    homeTeam = homeSeasonTeam != null ? DbSeasonTeamToTeamMapper.Map(homeSeasonTeam) : new Team(),
                    awayTeam = awaySeasonTeam != null ? DbSeasonTeamToTeamMapper.Map(awaySeasonTeam) : new Team(),
                },
                modelHomeOdds = dbOdds.HomeOdds,
                modelAwayOdds = dbOdds.AwayOdds,
                modelName = dbOdds.ModelName,
            });
        }

        return gameOddsList;
    }
}
