using Entities.DbModels;

namespace DatabaseAccess.SnapshotRepository;

public interface ISnapshotRepository
{
    Task<bool> PartnerOddsVersionExists(string country, DateTime partnerUpdatedUtc);
    Task AddPartnerOdds(IEnumerable<DbPartnerOdds> odds);
    /// <summary>Every saved version of these articles (text left out).</summary>
    Task<List<DbNhlArticle>> GetArticleVersions(IEnumerable<string> entityIds);
    Task AddArticle(DbNhlArticle article);
    Task SetArticleLastUpdated(string entityId, string contentHash, DateTime? lastUpdated);
    Task<List<DbNhlArticleTag>> GetArticleTags(string entityId);
    Task AddArticleTags(IEnumerable<DbNhlArticleTag> tags);
}