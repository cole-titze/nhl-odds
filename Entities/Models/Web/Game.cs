using Entities.Types;
using Entities.Types.Enums;

namespace Entities.Models.Web;

public class Game
{
    public int Id { get; set; }
    public DateTime GameDate { get; set; }
    public int HomeGoals { get; set; }
    public int AwayGoals { get; set; }
    public int SeasonStartYear { get; set; }
    public Winner Winner { get; set; }
    public PeriodType EndPeriod { get; set; }
    public bool HasBeenPlayed { get; set; }
    public Team HomeTeam { get; set; } = new Team();
    public Team AwayTeam { get; set; } = new Team();

    public int IsWin(int teamId)
    {
        if (!HasBeenPlayed)
            return 0;

        if ((HomeTeam.Id == teamId && Winner == Winner.HOME) || (AwayTeam.Id == teamId && Winner == Winner.AWAY))
            return 1;

        return 0;
    }

    public int IsLoss(int teamId)
    {
        if (!HasBeenPlayed)
            return 0;

        if ((HomeTeam.Id == teamId && Winner != Winner.HOME) || (AwayTeam.Id == teamId && Winner != Winner.AWAY))
            return 1;

        return 0;
    }

    public int IsOvertimeLoss(int teamId)
    {
        if (!HasBeenPlayed)
            return 0;

        if (EndPeriod == PeriodType.Regulation)
            return 0;

        return IsLoss(teamId);
    }
}
