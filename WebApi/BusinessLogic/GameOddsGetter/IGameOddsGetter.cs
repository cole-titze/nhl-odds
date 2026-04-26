using Entities.Models.Web;
using Entities.Types;
using Entities.ViewModels;

namespace WebApi.BusinessLogic.GameOddsGetter;

public interface IGameOddsGetter
{
    Task<IEnumerable<GameOddsVM>> GetGameOddsInDateRange(DateRange dateRange, int seasonStartYear);
    Task<IEnumerable<TeamStats>> BuildAllTeamsGameOdds(IEnumerable<TeamStats> teams, int seasonStartYear);
    Task<TeamStats> BuildTeamGameOdds(TeamStats team, int seasonStartYear);
    Task<AnchorDateVM> GetAnchorDate(int seasonStartYear);
}
