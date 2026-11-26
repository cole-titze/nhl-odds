using Entities.Models.Web;

namespace DatabaseAccess.WebTeamRepository;

public interface ITeamRepository
{
    Task<IEnumerable<TeamStats>> GetAllTeams(int seasonStartYear);
    Task<TeamStats?> GetTeam(int teamId, int seasonStartYear);
    /// <summary>The latest season the team has a record for (a former team's last one), or null for an unknown team.</summary>
    Task<int?> GetLastSeason(int teamId);
}
