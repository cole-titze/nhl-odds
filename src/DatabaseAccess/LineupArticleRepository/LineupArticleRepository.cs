using Entities.DbModels;
using Microsoft.EntityFrameworkCore;

namespace DatabaseAccess.LineupArticleRepository;

public class LineupArticleRepository : ILineupArticleRepository
{
    private readonly NhlDbContext _dbContext;

    public LineupArticleRepository(NhlDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> Exists(string contentHash)
    {
        return _dbContext.LineupArticle.AnyAsync(a => a.ContentHash == contentHash);
    }

    public async Task Add(DbLineupArticle article)
    {
        _dbContext.LineupArticle.Add(article);
        await _dbContext.SaveChangesAsync();
    }
}