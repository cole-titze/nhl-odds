namespace Entities.DbModels;

/// <summary>
/// A player (or other NHL entity) linked in the text of one version of an NHL.com article
/// (&lt;forge-entity slug="jackson-blake-8482809" code="player"&gt;).
/// </summary>
public class DbNhlArticleMention
{
    public string EntityId { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    // The link's code, e.g. "player"
    public string Code { get; set; } = string.Empty;
    // The NHL id at the end of the link's slug
    public long NhlId { get; set; }
    public string Title { get; set; } = string.Empty;
    // How many times the article links it
    public int Count { get; set; }
}