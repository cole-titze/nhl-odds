using Entities.Models;

namespace Entities.DbModels.Mappers
{
    public static class MapGameToDbGameOfficial
    {
        public static IEnumerable<DbGameOfficial> Map(Game game)
        {
            if (game.rosterStats == null || game.rosterStats.linesmen == null || game.rosterStats.referees == null)
                throw new ArgumentNullException(nameof(game.rosterStats), "Game roster stats and officials can not be null.");

            var gameOfficials = new List<DbGameOfficial>();
            foreach (var linesman in game.rosterStats.linesmen)
            {
                var dbGameOfficial = new DbGameOfficial()
                {
                    gameId = game.id,
                    role = Role.Linesman,
                    name = linesman.name,
                };
                gameOfficials.Add(dbGameOfficial);
            }

            foreach (var referee in game.rosterStats.referees)
            {
                var dbGameOfficial = new DbGameOfficial()
                {
                    gameId = game.id,
                    role = Role.Referee,
                    name = referee.name,
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
}