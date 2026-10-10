namespace Entities.DbModels;

/// <summary>
/// One game's section of NHL.com's lineup projections article, saved the first time its text is seen. The article is
/// written a game at a time, so each section is its own version: the latest one before puck drop is the final projection.
/// </summary>
public class DbLineupGame
{
    // SHA-256 (lowercase hex) of the section's markdown
    public string SectionHash { get; set; } = string.Empty;
    // The LineupArticle version it was first seen in
    public string ArticleHash { get; set; } = string.Empty;
    public DateTime FirstSeenUTC { get; set; }
    // Null when no scheduled game matched the two teams
    public int? GameId { get; set; }
    public int? AwayTeamId { get; set; }
    public int? HomeTeamId { get; set; }
    // The section heading, e.g. "FLYERS (0-3-2) at BRUINS (3-2-0)"
    public string Heading { get; set; } = string.Empty;
    // The "Status report" paragraph (injury and practice notes)
    public string? StatusReport { get; set; }
}