namespace Entities.ViewModels;

public class BookmakerOddsVM
{
    public string BookmakerName { get; set; } = string.Empty;
    public double HomeOdds { get; set; }
    public double AwayOdds { get; set; }
    public double HomePoint { get; set; }
    public int HomePrice { get; set; }
    public double AwayPoint { get; set; }
    public int AwayPrice { get; set; }
    public double OverUnderPoint { get; set; }
    public int OverPrice { get; set; }
    public int UnderPrice { get; set; }
}
