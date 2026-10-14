using Entities.ViewModels;

namespace WebApi.Mappers;

public static class TeamsVmToSeasonTeamsVmMapper
{
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
            BookmakerOdds = game.BookmakerOdds.Select(b => new SeasonTeamGameOddsVM
            {
                BookmakerName = b.BookmakerName,
                HomeOdds = b.HomeOdds,
                AwayOdds = b.AwayOdds,
            }).ToList(),
        };
    }
}