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

    public async Task<int> GetGameCountForSeason(int seasonStartYear)
    {
        return await _dbContext.GameRaw
            .Where(g => g.SeasonStartYear == seasonStartYear)
            .CountAsync();
    }

    public async Task<int> GetGamesWithOddsCountForSeason(int seasonStartYear)
    {
        var seasonGameIds = _dbContext.GameRaw
            .Where(g => g.SeasonStartYear == seasonStartYear)
            .Select(g => g.Id);

        return await _dbContext.BookmakerOdds
            .Where(o => seasonGameIds.Contains(o.GameId))
            .Select(o => o.GameId)
            .Distinct()
            .CountAsync();
    }

    public async Task AddOrUpdateBookmakerOdds(List<DbBookmakerOdds> odds)
    {
        var gameIds = odds.Select(o => o.GameId).Distinct().ToHashSet();
        var existing = await _dbContext.BookmakerOdds
            .Where(o => gameIds.Contains(o.GameId))
            .ToListAsync();
        var existingByKey = existing.ToDictionary(o => (o.GameId, o.BookmakerName));

        foreach (var record in odds)
        {
            if (existingByKey.TryGetValue((record.GameId, record.BookmakerName), out var db))
            {
                if (!db.IsEquivalentTo(record))
                    db.Clone(record);
            }
            else
            {
                await _dbContext.BookmakerOdds.AddAsync(record);
                existingByKey[(record.GameId, record.BookmakerName)] = record;
            }
        }
    }

    public async Task AddOrUpdateBookmakerSpreads(List<DbBookmakerSpreads> spreads)
    {
        var gameIds = spreads.Select(o => o.GameId).Distinct().ToHashSet();
        var existing = await _dbContext.BookmakerSpreads
            .Where(o => gameIds.Contains(o.GameId))
            .ToListAsync();
        var existingByKey = existing.ToDictionary(o => (o.GameId, o.BookmakerName));

        foreach (var record in spreads)
        {
            if (existingByKey.TryGetValue((record.GameId, record.BookmakerName), out var db))
            {
                if (!db.IsEquivalentTo(record))
                    db.Clone(record);
            }
            else
            {
                await _dbContext.BookmakerSpreads.AddAsync(record);
                existingByKey[(record.GameId, record.BookmakerName)] = record;
            }
        }
    }

    public async Task AddOrUpdateBookmakerTotals(List<DbBookmakerTotals> totals)
    {
        var gameIds = totals.Select(o => o.GameId).Distinct().ToHashSet();
        var existing = await _dbContext.BookmakerTotals
            .Where(o => gameIds.Contains(o.GameId))
            .ToListAsync();
        var existingByKey = existing.ToDictionary(o => (o.GameId, o.BookmakerName));

        foreach (var record in totals)
        {
            if (existingByKey.TryGetValue((record.GameId, record.BookmakerName), out var db))
            {
                if (!db.IsEquivalentTo(record))
                    db.Clone(record);
            }
            else
            {
                await _dbContext.BookmakerTotals.AddAsync(record);
                existingByKey[(record.GameId, record.BookmakerName)] = record;
            }
        }
    }

    public async Task SaveRawResponse(string rawJson)
    {
        await _dbContext.BookmakerOddsResponse.AddAsync(new DbBookmakerOddsResponse
        {
            FetchedDateUTC = DateTime.UtcNow,
            RawJson = rawJson,
        });
    }

    public async Task SaveRawResponse(string rawJson, DateTime queryDateUtc)
    {
        await _dbContext.BookmakerOddsResponse.AddAsync(new DbBookmakerOddsResponse
        {
            FetchedDateUTC = DateTime.UtcNow,
            QueryDateUTC = queryDateUtc,
            RawJson = rawJson,
        });
    }

    public async Task<DbBookmakerOddsResponse?> GetCachedResponse(DateTime queryDateUtc)
    {
        return await _dbContext.BookmakerOddsResponse
            .Where(r => r.QueryDateUTC == queryDateUtc)
            .OrderByDescending(r => r.FetchedDateUTC)
            .FirstOrDefaultAsync();
    }

    public async Task Commit()
    {
        await _dbContext.SaveChangesAsync();
    }
}