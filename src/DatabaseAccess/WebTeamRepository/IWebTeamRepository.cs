using Entities.Models.Web;

namespace DatabaseAccess.WebTeamRepository;

public interface ITeamRepository
{
    Task<IEnumerable<TeamStats>> GetAllTeams(int seasonStartYear);
    Task<TeamStats> GetTeam(int teamId, int seasonStartYear);
}