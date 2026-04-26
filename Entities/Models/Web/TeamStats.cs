namespace Entities.Models.Web;

public class TeamStats
{
    public Team Team { get; set; } = new Team();
    public double ModelLogLoss { get; set; }
    public IEnumerable<GameOdds> GameOdds { get; set; } = new List<GameOdds>();
}