using DatabaseAccess.WebTeamRepository;
using Entities.Models.Web;

namespace WebApi.Tests.BusinessLogic.Fakes;

public class FakeTeamRepository : ITeamRepository
{
    private readonly IEnumerable<TeamStats> _teams;
    public FakeTeamRepository(List<TeamStats> teams)
    {
        _teams = teams;
    }
    public Task<IEnumerable<TeamStats>> GetAllTeams(int seasonStartYear)
    {
        return Task.FromResult(_teams);
    }

    public Task<TeamStats?> GetTeam(int teamId, int seasonStartYear)
    {
        return Task.FromResult(_teams.FirstOrDefault(t => t.Team.Id == teamId));
    }

    public Task<int?> GetLastSeason(int teamId)
    {
        return Task.FromResult(_teams.Any(t => t.Team.Id == teamId) ? (int?)2025 : null);
    }
}
