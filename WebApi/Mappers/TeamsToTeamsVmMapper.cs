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
        teamsVm.teams = teams;
        teamsVm = BuildSeasonTotals(teamsVm);

        return teamsVm;
    }

    private static TeamsVM BuildSeasonTotals(TeamsVM teamsVm)
    {
        foreach (var team in teamsVm.teams)
        {
            teamsVm.seasonTotals.totalGameCount += team.totalGameCount;
            teamsVm.seasonTotals.totalModelAccurateGameCount += team.totalModelAccurateGameCount;
            teamsVm.seasonTotals.modelLogLoss += (team.modelLogLoss * team.totalGameCount);
        }
        if (teamsVm.seasonTotals.totalGameCount == 0)
            return teamsVm;

        teamsVm.seasonTotals.modelLogLoss /= teamsVm.seasonTotals.totalGameCount;

        // Cut in half since we were counting games twice (home and away)
        teamsVm.seasonTotals.totalGameCount /= 2;
        teamsVm.seasonTotals.totalModelAccurateGameCount /= 2;

        return teamsVm;
    }
}
