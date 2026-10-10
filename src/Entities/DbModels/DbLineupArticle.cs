namespace Entities.DbModels;

/// <summary>
/// One version of NHL.com's daily lineup projections article. The article is rewritten in place every day, so a
/// version is saved the first time its text is seen; saves that only change metadata aren't new versions.
/// </summary>
public class DbLineupArticle
{
    // SHA-256 (lowercase hex) of the article's markdown parts joined with "\n"
    public string ContentHash { get; set; } = string.Empty;
    public DateTime FirstSeenUTC { get; set; }
    public DateTime? ContentDate { get; set; }
    public DateTime? LastUpdated { get; set; }
    public string RawJson { get; set; } = string.Empty;
}