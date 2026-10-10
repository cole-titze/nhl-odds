using Entities.DbModels;

namespace DatabaseAccess.LineupArticleRepository;

public interface ILineupArticleRepository
{
    Task<bool> Exists(string contentHash);
    Task Add(DbLineupArticle article);
}