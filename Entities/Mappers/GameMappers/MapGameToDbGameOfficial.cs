using Entities.DbModels;
using Entities.Models;

namespace Entities.Mappers.GameMappers;

public static class MapGameToDbGameOfficial
{
    public static IEnumerable<DbGameOfficial> Map(Game game)
    {
        if (game.RosterStats == null || game.RosterStats.Linesmen == null || game.RosterStats.Referees == null)
            throw new ArgumentNullException(nameof(game.RosterStats), "Game roster stats and officials can not be null.");

        var gameOfficials = new List<DbGameOfficial>();
        foreach (var linesman in game.RosterStats.Linesmen)
        {
            var dbGameOfficial = new DbGameOfficial()
            {
                GameId = game.Id,
                Role = Role.Linesman,
                Name = linesman.Name,
            };
            gameOfficials.Add(dbGameOfficial);
        }

        foreach (var referee in game.RosterStats.Referees)
        {
            var dbGameOfficial = new DbGameOfficial()
            {
                GameId = game.Id,
                Role = Role.Referee,
                Name = referee.Name,
            };
            gameOfficials.Add(dbGameOfficial);
        }

        return gameOfficials;
    }
    public static IEnumerable<DbGameOfficial> MapList(IEnumerable<Game> games)
    {
        var gamesOfficials = new List<DbGameOfficial>();
        foreach (var game in games)
        {
            var gameOfficials = Map(game);
            gamesOfficials.AddRange(gameOfficials);
        }

        return gamesOfficials;
    }
}