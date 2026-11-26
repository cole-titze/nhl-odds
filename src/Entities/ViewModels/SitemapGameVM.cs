namespace Entities.ViewModels;

/// <summary>A game for the sitemap: the parts of its page's URL (/game/2026020123/oilers-at-maple-leafs-2026-11-21),
/// its season and teams (for that season's team pages) and whether it's been played (its lastmod is its day).</summary>
public class SitemapGameVM
{
    public int Id { get; set; }
    public int SeasonStartYear { get; set; }
    public DateTime GameDate { get; set; }
    public int AwayTeamId { get; set; }
    public string AwayTeamName { get; set; } = string.Empty;
    public int HomeTeamId { get; set; }
    public string HomeTeamName { get; set; } = string.Empty;
    public bool HasBeenPlayed { get; set; }
}
