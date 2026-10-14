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
        return new SeasonTeamsVM
        {
            Teams = summary.Teams,
            SeasonTotals = summary.SeasonTotals,
            Games = teamsVm.Teams
                .SelectMany(t => t.GameOddsVM)
                .DistinctBy(g => g.Id)
                .Select(Map)
                .ToList(),
        };
    }

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