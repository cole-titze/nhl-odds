namespace Entities.Models
{
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
            Games = games.Where(x => x.homeTeamId == teamId || x.awayTeamId == teamId)
                .OrderBy(i => i.gameDateUTC)
                .Reverse()
                .ToList();
            HomeGames = Games.Where(x => x.homeTeamId == teamId).ToList();
            AwayGames = Games.Where(x => x.awayTeamId == teamId).ToList();

            var currentSeason = Games.First().seasonStartYear;
            CurrentSeasonGames = Games.Where(x => x.seasonStartYear == currentSeason).ToList();
        }
    }
}
