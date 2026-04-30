using Entities.Models.GamePlayEvents;
using Entities.Types;
using Entities.Types.Enums;

namespace Entities.Models;

public class Game
{
    // If no game has been played set default as ~4 days of rest (season hasn't started)
    public static readonly int DEFAULT_HOURS = 100;

    public int Id { get; set; }
    public int HomeTeamId { get; set; }
    public int AwayTeamId { get; set; }
    public string HomeTeamAbbr { get; set; } = string.Empty;
    public string AwayTeamAbbr { get; set; } = string.Empty;
    public int SeasonStartYear { get; set; }
    public DateTime GameDateUTC { get; set; }
    public int HomeGoals { get; set; }
    public int AwayGoals { get; set; }
    public Winner Winner { get; set; }
    public PeriodType EndPeriod { get; set; }
    public int HomeSOG { get; set; }
    public int AwaySOG { get; set; }
    public int HomePPG { get; set; }
    public int AwayPPG { get; set; }
    public int HomePIM { get; set; }
    public int AwayPIM { get; set; }
    public double HomeFaceOffWinPercent { get; set; }
    public double AwayFaceOffWinPercent { get; set; }
    public int HomeBlockedShots { get; set; }
    public int AwayBlockedShots { get; set; }
    public int HomeHits { get; set; }
    public int AwayHits { get; set; }
    public int HomeTakeaways { get; set; }
    public int AwayTakeaways { get; set; }
    public int HomeGiveaways { get; set; }
    public int AwayGiveaways { get; set; }
    public bool HasBeenPlayed { get; set; }
    public GameType GameType { get; set; } = GameType.Regular;
    public GameRosterStats? RosterStats { get; set; }
    public GameExtendedInfo? ExtendedInfo { get; set; }
    public GameEvents? GameEvents { get; set; }

    /// <summary>
    /// Gets if the team won or not
    /// </summary>
    /// <param name="teamId"></param>
    /// <returns>True if the team won, otherwise false</returns>
    public bool IsWinner(int teamId)
    {
        if (HomeTeamId == teamId && Winner == Winner.HOME) return true;
        if (AwayTeamId == teamId && Winner == Winner.AWAY) return true;
        return false;
    }
    /// <summary>
    /// Gets the hours between the current game and game passed in
    /// </summary>
    /// <param name="game">Game to find hours between.</param>
    /// <returns></returns>
    public double GetHoursBetweenGames(Game? game)
    {
        if (game == null)
            return DEFAULT_HOURS;
        var hourDifference = (GameDateUTC - game.GameDateUTC).TotalHours;

        return Math.Abs(hourDifference);
    }
}