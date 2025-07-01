using Entities.Models;
using Entities.Models.Teams;

namespace DatabaseAccess.TeamRepository;

public interface ITeamRepository
{
    Task AddUpdateSeasonTeams(IEnumerable<SeasonTeam> teams);
    Task AddUpdateTeams(IEnumerable<Team> teams);
    Task<bool> HasSeasonTeams(int seasonStartYear);
}
