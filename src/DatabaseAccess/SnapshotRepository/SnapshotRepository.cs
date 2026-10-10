using Entities.DbModels;
using Microsoft.EntityFrameworkCore;

namespace DatabaseAccess.SnapshotRepository;

public class SnapshotRepository : ISnapshotRepository
{
    private readonly NhlDbContext _dbContext;

    public SnapshotRepository(NhlDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> PartnerOddsVersionExists(string country, DateTime partnerUpdatedUtc)
    {
        return _dbContext.PartnerOdds.AnyAsync(o => o.Country == country && o.PartnerUpdatedUTC == partnerUpdatedUtc);
    }

    public Task AddPartnerOdds(IEnumerable<DbPartnerOdds> odds)
    {
        _dbContext.PartnerOdds.AddRange(odds);
        return Save();
    }

    public Task<List<DbNhlArticle>> GetArticleVersions(IEnumerable<string> entityIds)
    {
        var ids = entityIds.ToList();
        return _dbContext.NhlArticle.AsNoTracking()
            .Where(a => ids.Contains(a.EntityId))
            .Select(a => new DbNhlArticle { EntityId = a.EntityId, ContentHash = a.ContentHash, LastUpdated = a.LastUpdated })
            .ToListAsync();
    }

    public async Task AddArticle(DbNhlArticle article)
    {
        _dbContext.NhlArticle.Add(article);
        await Save();
    }

    public Task SetArticleLastUpdated(string entityId, string contentHash, DateTime? lastUpdated)
    {
        return _dbContext.NhlArticle
            .Where(a => a.EntityId == entityId && a.ContentHash == contentHash)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.LastUpdated, lastUpdated));
    }

    public Task<List<DbNhlArticleTag>> GetArticleTags(string entityId)
    {
        return _dbContext.NhlArticleTag.AsNoTracking().Where(t => t.EntityId == entityId).ToListAsync();
    }

    public async Task AddArticleTags(IEnumerable<DbNhlArticleTag> tags)
    {
        _dbContext.NhlArticleTag.AddRange(tags);
        await Save();
    }

    // Clears tracking even when the save fails, so one bad insert doesn't break the next snapshot step
    private async Task Save()
    {
        try
        {
            await _dbContext.SaveChangesAsync();
        }
        finally
        {
            _dbContext.ChangeTracker.Clear();
        }
    }
}