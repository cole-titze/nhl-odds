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

            var logLoss = dbOdds.LogLoss != 0
                ? dbOdds.LogLoss
                : game.HasBeenPlayed
                    ? GameOdds.CalculateLogLoss(dbOdds.HomeOdds, dbOdds.AwayOdds, game.Winner)
                    : 0;

            gameOddsList.Add(new GameOdds
            {
                Game = new Game
                {
                    Id = game.Id,
                    GameDate = DateTime.SpecifyKind(game.GameDateUTC, DateTimeKind.Utc),
                    HomeGoals = game.HomeGoals,
                    AwayGoals = game.AwayGoals,
                    SeasonStartYear = game.SeasonStartYear,
                    Winner = game.Winner,
                    HasBeenPlayed = game.HasBeenPlayed,
                    HomeTeam = homeSeasonTeam != null ? DbSeasonTeamToTeamMapper.Map(homeSeasonTeam) : new Team(),
                    AwayTeam = awaySeasonTeam != null ? DbSeasonTeamToTeamMapper.Map(awaySeasonTeam) : new Team(),
                },
                ModelHomeOdds = dbOdds.HomeOdds,
                ModelAwayOdds = dbOdds.AwayOdds,
                ModelId = dbOdds.ModelId,
                LogLoss = logLoss,
            });
        }

        return gameOddsList;
    }
}
