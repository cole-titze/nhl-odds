namespace Entities.Models.Web;

/// <summary>A player who has played in a season, and when his last game that season started (UTC).</summary>
public class ActivePlayer
{
    public int Id { get; set; }
    public DateTime LastGameDateUTC { get; set; }
}
