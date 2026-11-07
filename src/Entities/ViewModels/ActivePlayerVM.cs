namespace Entities.ViewModels;

/// <summary>A player with a game in the season, for the sitemap: his page's lastmod is his last game.</summary>
public class ActivePlayerVM
{
    public int Id { get; set; }
    public DateTime LastGameDate { get; set; }
}
