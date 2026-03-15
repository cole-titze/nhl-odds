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

        var dbSpreads = await _dbContext.BookmakerSpreads
            .Where(x => gameIdSet.Contains(x.GameId))
            .ToListAsync();

        var dbTotals = await _dbContext.BookmakerTotals
            .Where(x => gameIdSet.Contains(x.GameId))
            .ToListAsync();

        var spreadsLookup = dbSpreads
            .ToDictionary(s => (s.GameId, s.BookmakerName));

        var totalsLookup = dbTotals
            .ToDictionary(t => (t.GameId, t.BookmakerName));

        return dbOdds
            .GroupBy(x => x.GameId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(o =>
                {
                    var odds = new BookmakerGameOdds
                    {
                        BookmakerName = o.BookmakerName,
                        HomeOdds = o.HomeOdds,
                        AwayOdds = o.AwayOdds,
                    };

                    if (spreadsLookup.TryGetValue((o.GameId, o.BookmakerName), out var spread))
                    {
                        odds.HomePoint = spread.HomePoint;
                        odds.HomePrice = spread.HomePrice;
                        odds.AwayPoint = spread.AwayPoint;
                        odds.AwayPrice = spread.AwayPrice;
                    }

                    if (totalsLookup.TryGetValue((o.GameId, o.BookmakerName), out var total))
                    {
                        odds.OverUnderPoint = total.OverUnderPoint;
                        odds.OverPrice = total.OverPrice;
                        odds.UnderPrice = total.UnderPrice;
                    }

                    return odds;
                }).ToList()
            );
    }
}
