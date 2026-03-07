using DatabaseAccess.WebTeamRepository;
using Entities.Models.Web;

namespace WebApi.BusinessLogic.TeamGetter;

public class TeamGetter : ITeamGetter
{
    private readonly ITeamRepository _teamRepository;

    public TeamGetter(ITeamRepository teamRepository)
    {
        _teamRepository = teamRepository;
    }

    public async Task<IEnumerable<TeamStats>> GetAllTeamsStats(int seasonStartYear)
    {
        return await _teamRepository.GetAllTeams(seasonStartYear);
    }

    public async Task<TeamStats> GetTeamStats(int teamId, int seasonStartYear)
    {
        return await _teamRepository.GetTeam(teamId, seasonStartYear);
    }
}
