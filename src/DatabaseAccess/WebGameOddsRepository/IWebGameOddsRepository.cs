using Entities.Models.Web;
using Entities.Types;

namespace DatabaseAccess.WebGameOddsRepository;

public interface IGameOddsRepository
{
    Task<IEnumerable<GameOdds>> GetGameOddsInDateRange(DateRange dateRange, int seasonStartYear);
    Task<IEnumerable<GameOdds>> GetTeamGameOdds(int teamId, int seasonStartYear);
    Task<GameOdds?> GetGameOdds(int gameId);
    Task<List<GameOdds>> GetAllGameOddsForSeason(int seasonStartYear);
    Task<DateTime?> GetAnchorDate(int seasonStartYear);
}