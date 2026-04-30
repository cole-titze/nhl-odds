using Entities.Types;

namespace Entities.Models.Web;

public class GameOdds
{
    public double? ModelHomeOdds { get; set; }
    public double? ModelAwayOdds { get; set; }
    public double? LogLoss { get; set; }
    public int? ModelId { get; set; }
    public Game Game { get; set; } = new Game();
    public List<BookmakerGameOdds> BookmakerOdds { get; set; } = new();
    public double? PredictedSpread { get; set; }
    public double? SpreadCoverProb { get; set; }
    public double? PredictedTotal { get; set; }
    public double? TotalOverProb { get; set; }

    public static double CalculateLogLoss(double homeOdds, double awayOdds, Winner winner)
    {
        if (homeOdds <= 0 || awayOdds <= 0)
            return -1;

        // winner: HOME=0, AWAY=1
        // logLoss = -(winner * log(awayOdds) + (1 - winner) * log(homeOdds))
        int w = (int)winner;
        return -(w * Math.Log(awayOdds) + (1 - w) * Math.Log(homeOdds));
    }
}