using DatabaseAccess.WebBookmakerOddsRepository.Mappers;
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
            .AsNoTracking()
            .Where(x => gameIdSet.Contains(x.GameId))
            .ToListAsync();

        var dbSpreads = await _dbContext.BookmakerSpreads
            .AsNoTracking()
            .Where(x => gameIdSet.Contains(x.GameId))
            .ToListAsync();

        var dbTotals = await _dbContext.BookmakerTotals
            .AsNoTracking()
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
                    spreadsLookup.TryGetValue((o.GameId, o.BookmakerName), out var spread);
                    totalsLookup.TryGetValue((o.GameId, o.BookmakerName), out var total);
                    return DbBookmakerOddsToBookmakerGameOddsMapper.Map(o, spread, total);
                }).ToList()
            );
    }
}