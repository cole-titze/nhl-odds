namespace Entities.Models;

public class SeasonGames
{
    public readonly IDictionary<int, DateSortedTeamGames> GamesMap = new Dictionary<int, DateSortedTeamGames>();

    public SeasonGames(IEnumerable<Game> games)
    {
        foreach (var game in games)
        {
            GamesMap.TryAdd(game.HomeTeamId, new DateSortedTeamGames(games, game.HomeTeamId));
            GamesMap.TryAdd(game.AwayTeamId, new DateSortedTeamGames(games, game.AwayTeamId));
        }
    }
}