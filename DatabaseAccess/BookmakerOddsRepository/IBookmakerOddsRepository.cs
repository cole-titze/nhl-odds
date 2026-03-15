using Entities.DbModels;

namespace DatabaseAccess.BookmakerOddsRepository;

public interface IBookmakerOddsRepository
{
    Task<HashSet<int>> GetGameIdsWithBookmakerOdds();
    Task<int> GetGameCountForSeason(int seasonStartYear);
    Task<int> GetGamesWithOddsCountForSeason(int seasonStartYear);
    Task AddOrUpdateBookmakerOdds(List<DbBookmakerOdds> odds);
    Task AddOrUpdateBookmakerSpreads(List<DbBookmakerSpreads> spreads);
    Task AddOrUpdateBookmakerTotals(List<DbBookmakerTotals> totals);
    Task SaveRawResponse(string rawJson);
    Task SaveRawResponse(string rawJson, DateTime queryDateUtc);
    Task<DbBookmakerOddsResponse?> GetCachedResponse(DateTime queryDateUtc);
    Task Commit();
}
