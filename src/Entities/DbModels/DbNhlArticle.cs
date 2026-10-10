namespace Entities.DbModels;

/// <summary>
/// One version of an NHL.com article (league or team site). Articles can be edited or rewritten in place, so a
/// version is saved the first time its text is seen.
/// </summary>
public class DbNhlArticle
{
    // NHL.com's content id for the article (_entityId)
    public string EntityId { get; set; } = string.Empty;
    // SHA-256 (lowercase hex) of the article's markdown parts joined with "\n"
    public string ContentHash { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Headline { get; set; }
    public string? Summary { get; set; }
    public DateTime? ContentDate { get; set; }
    // The newest lastUpdatedDate seen with this text (metadata-only saves move it forward)
    public DateTime? LastUpdated { get; set; }
    public DateTime FirstSeenUTC { get; set; }
    // The markdown parts joined with "\n" (photos, videos and embeds left out)
    public string Body { get; set; } = string.Empty;
}