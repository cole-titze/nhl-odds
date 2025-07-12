using Entities.Types;

namespace Entities.Models;

public class GameSkaterStats : IGamePlayerStats
{
    public int PlayerId { get; set; }
    public int TeamId { get; set; }
    public int Goals { get; set; }
    public int Assists { get; set; }
    public int ShotsOnGoal { get; set; }
    public int BlockedShots { get; set; }
    public int PenaltyMinutes { get; set; }
    public int PowerPlayGoals { get; set; }
    public int PlusMinus { get; set; }
    public double FaceOffWinningPctg { get; set; }
    public int Hits { get; set; }
    public int Giveaways { get; set; }
    public int Takeaways { get; set; }
    public int TimeOnIceSeconds { get; set; }
    // Default to stop null warning left-wing has no special rules
    public POSITION Position { get; set; } = POSITION.LeftWing;
}
