using Entities.DbModels;
using Entities.Models;

namespace DataCleaner;

public static class CleanedGameListExtensions
{
    public static IEnumerable<Game> GetNewGames(this IEnumerable<DbGameCleaned> cleanedGames, IEnumerable<Game> games)
    {
        var cleanedSet = new HashSet<int>(cleanedGames.Select(x => x.GameId));
        return games.Where(g => !cleanedSet.Contains(g.Id)).ToList();
    }
}