namespace Entities.ViewModels;

/// <summary>A player and when their last game started, for the sitemap: the page's lastmod is that game's day.</summary>
public class ActivePlayerVM
{
    public int Id { get; set; }
    public DateTime LastGameDate { get; set; }
}
