namespace Entities.DbModels;

/// <summary>
/// A tag on an NHL.com article. Team, player and game tags carry the NHL id (teamid-10 → 10), which links the
/// article to the Team, Player and Game tables.
/// </summary>
public class DbNhlArticleTag
{
    public string EntityId { get; set; } = string.Empty;
    public string TagSlug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    // The tag's source: "team", "player", "game", "taxonomy", ...
    public string? SourceName { get; set; }
    // "team", "player" or "game" when the slug is teamid-N, playerid-N or gameid-N
    public string? IdType { get; set; }
    public long? NhlId { get; set; }
}