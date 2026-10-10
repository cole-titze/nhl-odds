namespace Entities.DbModels;

/// <summary>Marks a LineupArticle version as parsed into LineupGame/LineupPlayer rows (or as failed, once).</summary>
public class DbLineupArticleParse
{
    public string ArticleHash { get; set; } = string.Empty;
    public DateTime ParsedUTC { get; set; }
    public int Sections { get; set; }
    public string? Error { get; set; }
}