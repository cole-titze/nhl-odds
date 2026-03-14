using Entities.DbModels;

namespace DatabaseAccess.BookmakerOddsRepository;

public interface IBookmakerOddsRepository
{
    Task<HashSet<int>> GetGameIdsWithBookmakerOdds();
    Task AddBookmakerOdds(List<DbBookmakerOdds> odds);
    Task SaveRawResponse(string rawJson);
    Task Commit();
}
