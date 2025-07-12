using Entities.DbModels;
using Entities.Models;

namespace Entities.Mappers.GameMappers;

public static class MapGameToDbGameCoaches
{
    public static IEnumerable<DbGameCoach> Map(Game game)
    {
        if (game.RosterStats == null || game.RosterStats.HomeTeamCoach == null || game.RosterStats.AwayTeamCoach == null)
            throw new ArgumentNullException(nameof(game.RosterStats), "Game roster stats and coaches can not be null.");

        var gameCoaches = new List<DbGameCoach>();
        var dbGameCoach = new DbGameCoach()
        {
            GameId = game.Id,
            Name = game.RosterStats.HomeTeamCoach.Name,
            TeamId = game.HomeTeamId,
        };
        gameCoaches.Add(dbGameCoach);
        dbGameCoach = new DbGameCoach()
        {
            GameId = game.Id,
            Name = game.RosterStats.AwayTeamCoach.Name,
            TeamId = game.AwayTeamId,
        };
        gameCoaches.Add(dbGameCoach);

        return gameCoaches;
    }
    public static IEnumerable<DbGameCoach> MapList(IEnumerable<Game> games)
    {
        var gamesCoaches = new List<DbGameCoach>();
        foreach (var game in games)
        {
            var gameCoaches = Map(game);
            gamesCoaches.AddRange(gameCoaches);
        }

        return gamesCoaches;
    }
}