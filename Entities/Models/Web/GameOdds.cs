using Entities.Types;

namespace Entities.Models.Web;

public class GameOdds
{
    public double modelHomeOdds { get; set; }
    public double modelAwayOdds { get; set; }
    public double logLoss { get; set; }
    public int modelName { get; set; }
    public Game game { get; set; } = new Game();

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
