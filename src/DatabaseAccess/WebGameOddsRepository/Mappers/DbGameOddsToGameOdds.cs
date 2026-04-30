using DatabaseAccess.WebTeamRepository.Mappers;
using Entities.DbModels;
using Entities.Models.Web;
using Entities.Types;

namespace DatabaseAccess.WebGameOddsRepository.Mappers;

public static class DbGameOddsToGameOddsMapper
{
    public static List<GameOdds> Map(
        IEnumerable<DbGameRaw> games,
        IDictionary<int, DbGameOdds> latestOddsByGameId,
        Dictionary<int, DbSeasonTeam> seasonTeams)
    {
        var gameOddsList = new List<GameOdds>();
        foreach (var game in games)
        {
            seasonTeams.TryGetValue(game.HomeTeamId, out var homeSeasonTeam);
            seasonTeams.TryGetValue(game.AwayTeamId, out var awaySeasonTeam);

            latestOddsByGameId.TryGetValue(game.Id, out var dbOdds);

            double? logLoss = null;
            if (dbOdds != null)
            {
                logLoss = dbOdds.LogLoss != 0
                    ? dbOdds.LogLoss
                    : game.HasBeenPlayed
                        ? GameOdds.CalculateLogLoss(dbOdds.HomeOdds, dbOdds.AwayOdds, game.Winner)
                        : 0;
            }

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
                    EndPeriod = game.EndPeriod,
                    HasBeenPlayed = game.HasBeenPlayed,
                    GameType = game.GameType,
                    HomeTeam = homeSeasonTeam != null ? DbSeasonTeamToTeamMapper.Map(homeSeasonTeam) : new Team(),
                    AwayTeam = awaySeasonTeam != null ? DbSeasonTeamToTeamMapper.Map(awaySeasonTeam) : new Team(),
                },
                ModelHomeOdds = dbOdds?.HomeOdds,
                ModelAwayOdds = dbOdds?.AwayOdds,
                ModelId = dbOdds?.ModelId,
                LogLoss = logLoss,
            });
        }

        return gameOddsList;
    }
}