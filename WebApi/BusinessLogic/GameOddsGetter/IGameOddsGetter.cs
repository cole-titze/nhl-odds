using Entities.Types;
using Entities.Models.Web;

namespace WebApi.BusinessLogic.GameOddsGetter;

public interface IGameOddsGetter
{
    Task<IEnumerable<TeamStats>> BuildTeamsGameOdds(IEnumerable<TeamStats> teams, int seasonStartYear);
    Task<TeamStats> BuildTeamGameOdds(TeamStats team, int seasonStartYear);
    Task<IEnumerable<GameOdds>> GetGameOddsInDateRange(DateRange dateRange, int seasonStartYear);
    Task<IEnumerable<GameOdds>> GetTeamGameOdds(int teamId, int seasonStartYear);
}
