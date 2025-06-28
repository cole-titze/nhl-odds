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
            .OrderBy(i => i.GameDateUTC)
            .Reverse()
            .ToList();
        HomeGames = Games.Where(x => x.HomeTeamId == teamId).ToList();
        AwayGames = Games.Where(x => x.AwayTeamId == teamId).ToList();

        var currentSeason = Games.First().SeasonStartYear;
        CurrentSeasonGames = Games.Where(x => x.SeasonStartYear == currentSeason).ToList();
    }
}
