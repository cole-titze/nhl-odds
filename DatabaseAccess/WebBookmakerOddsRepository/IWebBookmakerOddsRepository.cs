using Entities.Models.Web;

namespace DatabaseAccess.WebBookmakerOddsRepository;

public interface IWebBookmakerOddsRepository
{
    Task<Dictionary<int, List<BookmakerGameOdds>>> GetBookmakerOddsByGameIds(IEnumerable<int> gameIds);
}