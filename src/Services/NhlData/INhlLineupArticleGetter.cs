namespace Services.NhlData;

public interface INhlLineupArticleGetter
{
    /// <summary>The lineup projections article's raw JSON, or null if it can't be fetched.</summary>
    Task<string?> GetLineupArticle();
}