using Entities.Models.Web;

namespace WebApi.BusinessLogic.TeamGetter;

public interface ITeamGetter
{
    Task<IEnumerable<TeamStats>> GetAllTeamsStats(int seasonStartYear);
    Task<TeamStats> GetTeamStats(int teamId, int seasonStartYear);
}
