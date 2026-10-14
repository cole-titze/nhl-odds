using Entities.ViewModels;

namespace WebApi.Mappers;

public static class TeamsVmToSeasonTeamsVmMapper
{
    // The Teams page only compares the model with these books; the others made up most of the
    // response (1.6MB for a full season with every book)
    private static readonly HashSet<string> Bookmakers = ["DraftKings", "Kalshi"];

    public static SeasonTeamsVM Map(TeamsVM teamsVm)
    {
        var summary = TeamsVmToTeamsSummaryVmMapper.Map(teamsVm);
        var games = SeasonGames(teamsVm).ToList();
        return new SeasonTeamsVM
        {
            Teams = summary.Teams,
            SeasonTotals = summary.SeasonTotals,
            Games = games.Select(Map).ToList(),
            OtherBookmakers = games
                .SelectMany(g => g.BookmakerOdds)
                .Select(b => b.BookmakerName)
                .Where(name => !Bookmakers.Contains(name))
                .Distinct()
                .Order()
                .ToList(),
        };
    }

    // One book's odds for every game that has them
    public static List<SeasonBookmakerOddsVM> MapBookmakerOdds(TeamsVM teamsVm, string bookmakerName)
    {
        return SeasonGames(teamsVm)
            .SelectMany(g => g.BookmakerOdds
                .Where(b => b.BookmakerName == bookmakerName)
                .Take(1)
                .Select(b => new SeasonBookmakerOddsVM { GameId = g.Id, HomeOdds = b.HomeOdds, AwayOdds = b.AwayOdds }))
            .ToList();
    }

    // Each team lists its own games, so a game shows up once per team
    private static IEnumerable<GameOddsVM> SeasonGames(TeamsVM teamsVm) =>
        teamsVm.Teams.SelectMany(t => t.GameOddsVM).DistinctBy(g => g.Id);

    private static SeasonTeamGameVM Map(GameOddsVM game)
    {
        return new SeasonTeamGameVM
        {
            Id = game.Id,
            GameDate = game.GameDate,
            HasBeenPlayed = game.HasBeenPlayed,
            LogLoss = game.LogLoss,
            Winner = game.Winner,
            HomeTeamId = game.HomeTeam?.Id ?? 0,
            AwayTeamId = game.AwayTeam?.Id ?? 0,
            BookmakerOdds = game.BookmakerOdds.Where(b => Bookmakers.Contains(b.BookmakerName)).Select(b => new SeasonTeamGameOddsVM
            {
                BookmakerName = b.BookmakerName,
                HomeOdds = b.HomeOdds,
                AwayOdds = b.AwayOdds,
            }).ToList(),
        };
    }
}