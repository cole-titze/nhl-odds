namespace Entities.ViewModels;

/// <summary>A player, their name (for the page's URL) and when their last game started, for the sitemap: the page's
/// lastmod is that game's day.</summary>
public class ActivePlayerVM
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime LastGameDate { get; set; }
}
