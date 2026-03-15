using Entities.Models.Web;
using Entities.Types;

namespace DatabaseAccess.WebGameOddsRepository;

public interface IGameOddsRepository
{
    Task<IEnumerable<GameOdds>> GetGameOddsInDateRange(DateRange dateRange, int seasonStartYear);
    Task<IEnumerable<GameOdds>> GetTeamGameOdds(int teamId, int seasonStartYear);
    Task<List<GameOdds>> GetAllGameOddsForSeason(int seasonStartYear);
}
