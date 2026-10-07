using Entities.ViewModels;

namespace WebApi.Mappers;

public static class TeamsVmToTeamsSummaryVmMapper
{
    public static TeamsSummaryVM Map(TeamsVM teamsVm)
    {
        return new TeamsSummaryVM
        {
            Teams = teamsVm.Teams.Select(Map).ToList(),
            SeasonTotals = teamsVm.SeasonTotals,
        };
    }

    private static TeamSummaryVM Map(TeamVM team)
    {
        return new TeamSummaryVM
        {
            Id = team.Id,
            LocationName = team.LocationName,
            TeamName = team.TeamName,
            LogoUri = team.LogoUri,
            ModelLogLoss = team.ModelLogLoss,
            TotalGameCount = team.TotalGameCount,
            SeasonWins = team.SeasonWins,
            SeasonLosses = team.SeasonLosses,
            SeasonOvertimeLosses = team.SeasonOvertimeLosses,
            TotalModelAccurateGameCount = team.TotalModelAccurateGameCount,
            DraftKingsLogLoss = team.DraftKingsLogLoss,
            DraftKingsAccurateGameCount = team.DraftKingsAccurateGameCount,
            DraftKingsGameCount = team.DraftKingsGameCount,
        };
    }
}
