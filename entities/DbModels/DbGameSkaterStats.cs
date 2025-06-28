using System.ComponentModel.DataAnnotations.Schema;
using Entities.Types;

namespace Entities.DbModels;

public class DbGameSkaterStats : IDbGamePlayerStats
{
    public int GameId { get; set; }
    public int PlayerId { get; set; }
    public int TeamId { get; set; }
    public int Goals { get; set; }
    public int Assists { get; set; }
    public int PlusMinus { get; set; }
    public int PenaltyMinutes { get; set; }
    public int Hits { get; set; }
    public int PowerPlayGoals { get; set; }
    public int ShotsOnGoal { get; set; }
    public double FaceOffWinningPctg { get; set; }
    public int BlockedShots { get; set; }
    public int Giveaways { get; set; }
    public int Takeaways { get; set; }
    public int TimeOnIceSeconds { get; set; }
    public POSITION Position { get; set; } = POSITION.LeftWing;

    [ForeignKey(nameof(PlayerId))]
    public DbPlayer? Player { get; set; }
    [ForeignKey(nameof(GameId))]
    public DbGameRaw? Game { get; set; }
    [ForeignKey(nameof(TeamId))]
    public DbTeam? Team { get; set; }
    public void Clone(IDbGamePlayerStats gamePlayerStats)
    {
        if (gamePlayerStats is DbGameSkaterStats gameSkaterStats)
        {
            GameId = gameSkaterStats.GameId;
            PlayerId = gameSkaterStats.PlayerId;
            Goals = gameSkaterStats.Goals;
            Assists = gameSkaterStats.Assists;
            PlusMinus = gameSkaterStats.PlusMinus;
            PenaltyMinutes = gameSkaterStats.PenaltyMinutes;
            Hits = gameSkaterStats.Hits;
            PowerPlayGoals = gameSkaterStats.PowerPlayGoals;
            ShotsOnGoal = gameSkaterStats.ShotsOnGoal;
            FaceOffWinningPctg = gameSkaterStats.FaceOffWinningPctg;
            BlockedShots = gameSkaterStats.BlockedShots;
            Giveaways = gameSkaterStats.Giveaways;
            Takeaways = gameSkaterStats.Takeaways;
            TimeOnIceSeconds = gameSkaterStats.TimeOnIceSeconds;
            Player = gameSkaterStats.Player;
            Game = gameSkaterStats.Game;
            Team = gameSkaterStats.Team;
        }
        else
        {
            throw new InvalidOperationException("Invalid type passed to Clone.");
        }
    }
}