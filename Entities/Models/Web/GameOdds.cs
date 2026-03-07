namespace Entities.Models.Web;

public class GameOdds
{
    public double modelHomeOdds { get; set; }
    public double modelAwayOdds { get; set; }
    public string modelName { get; set; } = string.Empty;
    public Game game { get; set; } = new Game();
}
