using Entities.Models.Teams;

namespace DatabaseAccess.TeamRepository;

public interface ITeamRepository
{
    Task AddUpdateSeasonTeams(IEnumerable<Team> teams);
    Task AddUpdateTeams(IEnumerable<Team> teams);
    Task<bool> HasSeasonTeams(int seasonStartYear);
    Task Commit();
}
