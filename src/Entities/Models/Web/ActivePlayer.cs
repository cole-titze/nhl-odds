namespace Entities.Models.Web;

/// <summary>A player who has played (in a season, or ever), and when their last such game started (UTC).</summary>
public class ActivePlayer
{
    public int Id { get; set; }
    public DateTime LastGameDateUTC { get; set; }
}
