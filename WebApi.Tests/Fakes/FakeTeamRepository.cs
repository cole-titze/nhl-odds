using DatabaseAccess.WebTeamRepository;
using Entities.Models.Web;

namespace WebApi.Tests.BusinessLogic.Fakes;

public class FakeTeamRepository : ITeamRepository
{
    private IEnumerable<TeamStats> _teams { get; set; } = new List<TeamStats>();
    public FakeTeamRepository(List<TeamStats> teams)
    {
        _teams = teams;
    }
    public Task<IEnumerable<TeamStats>> GetAllTeams(int seasonStartYear)
    {
        return Task.FromResult(_teams);
    }

    public Task<TeamStats> GetTeam(int teamId, int seasonStartYear)
    {
        return Task.FromResult(_teams.First(t => t.team.id == teamId));
    }
}
