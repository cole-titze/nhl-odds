namespace Entities.Models.Web;

public class BookmakerGameOdds
{
    public string BookmakerName { get; set; } = string.Empty;
    public double HomeOdds { get; set; }
    public double AwayOdds { get; set; }
    // Spread
    public double HomePoint { get; set; }
    public int HomePrice { get; set; }
    public double AwayPoint { get; set; }
    public int AwayPrice { get; set; }
    // Totals
    public double OverUnderPoint { get; set; }
    public int OverPrice { get; set; }
    public int UnderPrice { get; set; }
}
