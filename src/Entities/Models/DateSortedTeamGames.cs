namespace Entities.Models;

public class DateSortedTeamGames
{
    public int TeamId { get; set; }
    public readonly IEnumerable<Game> HomeGames;
    public readonly IEnumerable<Game> AwayGames;
    public readonly IEnumerable<Game> Games;
    public readonly IEnumerable<Game> CurrentSeasonGames;

    public DateSortedTeamGames(IEnumerable<Game> games, int teamId)
    {
        TeamId = teamId;
        Games = games.Where(x => x.HomeTeamId == teamId || x.AwayTeamId == teamId)
            .OrderByDescending(i => i.GameDateUTC)
            .ToList();
        HomeGames = Games.Where(x => x.HomeTeamId == teamId).ToList();
        AwayGames = Games.Where(x => x.AwayTeamId == teamId).ToList();

        var firstGame = Games.FirstOrDefault();
        var currentSeason = firstGame?.SeasonStartYear ?? 0;
        CurrentSeasonGames = Games.Where(x => x.SeasonStartYear == currentSeason).ToList();
    }
}

public static class GameListExtensions
{
    public static IEnumerable<Game> GetGamesBeforeDate(this IEnumerable<Game> games, DateTime date)
    {
        return games.Where(i => i.GameDateUTC < date).ToList();
    }

    public static IEnumerable<Game> GetUniqueGames(this IEnumerable<Game> gamesToClean)
    {
        return gamesToClean.GroupBy(x => x.Id).Select(x => x.First()).ToList();
    }

    public static IEnumerable<Game> GetFutureGames(this IEnumerable<Game> games)
    {
        return games.Where(g => !g.HasBeenPlayed).ToList();
    }
}
