using Entities.Models.Web;
using Microsoft.EntityFrameworkCore;

namespace DatabaseAccess.WebBookmakerOddsRepository;

public class WebBookmakerOddsRepository : IWebBookmakerOddsRepository
{
    private readonly GameDbContext _dbContext;

    public WebBookmakerOddsRepository(GameDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Dictionary<int, List<BookmakerGameOdds>>> GetBookmakerOddsByGameIds(IEnumerable<int> gameIds)
    {
        var gameIdSet = gameIds.ToHashSet();

        var dbOdds = await _dbContext.BookmakerOdds
            .Where(x => gameIdSet.Contains(x.GameId))
            .ToListAsync();

        return dbOdds
            .GroupBy(x => x.GameId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(o => new BookmakerGameOdds
                {
                    BookmakerName = o.BookmakerName,
                    HomeOdds = o.HomeOdds,
                    AwayOdds = o.AwayOdds,
                }).ToList()
            );
    }
}
