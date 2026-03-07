using Entities.Types;

namespace Entities.Models.Web;

public class Game
{
    public int id { get; set; }
    public DateTime gameDate { get; set; }
    public int homeGoals { get; set; }
    public int awayGoals { get; set; }
    public int seasonStartYear { get; set; }
    public Winner winner { get; set; }
    public bool hasBeenPlayed { get; set; }
    public Team homeTeam { get; set; } = new Team();
    public Team awayTeam { get; set; } = new Team();

    public int IsWin(int teamId)
    {
        if (!hasBeenPlayed)
            return 0;

        if ((homeTeam.id == teamId && winner == Winner.HOME) || (awayTeam.id == teamId && winner == Winner.AWAY))
            return 1;

        return 0;
    }

    public int IsLoss(int teamId)
    {
        if (!hasBeenPlayed)
            return 0;

        if ((homeTeam.id == teamId && winner != Winner.HOME) || (awayTeam.id == teamId && winner != Winner.AWAY))
            return 1;

        return 0;
    }
}
