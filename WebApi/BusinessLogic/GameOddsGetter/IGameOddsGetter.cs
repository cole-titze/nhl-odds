using Entities.Models.Web;
using Entities.Types;

namespace WebApi.BusinessLogic.GameOddsGetter;

public interface IGameOddsGetter
{
    Task<IEnumerable<TeamStats>> BuildAllTeamsGameOdds(IEnumerable<TeamStats> teams, int seasonStartYear);
    Task<TeamStats> BuildTeamGameOdds(TeamStats team, int seasonStartYear);
    Task<IEnumerable<GameOdds>> GetGameOddsInDateRange(DateRange dateRange, int seasonStartYear);
    Task<IEnumerable<GameOdds>> GetTeamGameOdds(int teamId, int seasonStartYear);
}
