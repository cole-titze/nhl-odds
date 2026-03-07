using Entities.Models.Web;
using Entities.ViewModels;

namespace WebApi.Mappers;

public static class TeamsToTeamsVmMapper
{
    public static TeamsVM Map(IEnumerable<TeamStats> teamsStats)
    {
        var teams = new List<TeamVM>();
        var teamsVm = new TeamsVM();

        foreach (var teamStats in teamsStats)
        {
            var teamVm = TeamToTeamVmMapper.Map(teamStats);
            teams.Add(teamVm);
        }
        teamsVm.Teams = teams;
        teamsVm = BuildSeasonTotals(teamsVm);

        return teamsVm;
    }

    private static TeamsVM BuildSeasonTotals(TeamsVM teamsVm)
    {
        foreach (var team in teamsVm.Teams)
        {
            teamsVm.SeasonTotals.TotalGameCount += team.TotalGameCount;
            teamsVm.SeasonTotals.TotalModelAccurateGameCount += team.TotalModelAccurateGameCount;
            teamsVm.SeasonTotals.ModelLogLoss += (team.ModelLogLoss * team.TotalGameCount);
        }
        if (teamsVm.SeasonTotals.TotalGameCount == 0)
            return teamsVm;

        teamsVm.SeasonTotals.ModelLogLoss /= teamsVm.SeasonTotals.TotalGameCount;

        // Cut in half since we were counting games twice (home and away)
        teamsVm.SeasonTotals.TotalGameCount /= 2;
        teamsVm.SeasonTotals.TotalModelAccurateGameCount /= 2;

        return teamsVm;
    }
}
