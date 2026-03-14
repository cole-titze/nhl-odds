using Entities.DbModels;
using Microsoft.EntityFrameworkCore;

namespace DatabaseAccess.BookmakerOddsRepository;

public class BookmakerOddsRepository : IBookmakerOddsRepository
{
    private readonly NhlDbContext _dbContext;

    public BookmakerOddsRepository(NhlDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HashSet<int>> GetGameIdsWithBookmakerOdds()
    {
        var gameIds = await _dbContext.BookmakerOdds
            .Select(x => x.GameId)
            .Distinct()
            .ToListAsync();
        return gameIds.ToHashSet();
    }

    public async Task AddBookmakerOdds(List<DbBookmakerOdds> odds)
    {
        await _dbContext.BookmakerOdds.AddRangeAsync(odds);
    }

    public async Task SaveRawResponse(string rawJson)
    {
        await _dbContext.BookmakerOddsResponse.AddAsync(new DbBookmakerOddsResponse
        {
            FetchedDateUTC = DateTime.UtcNow,
            RawJson = rawJson,
        });
    }

    public async Task Commit()
    {
        await _dbContext.SaveChangesAsync();
    }
}
