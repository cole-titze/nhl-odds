using Entities.Types;

namespace Entities.Models;

public class GameGoalieStats : IGamePlayerStats
{
    public int PlayerId { get; set; }
    public int TeamId { get; set; }
    public int EvenStrengthShotsSaved { get; set; }
    public int PowerPlayShotsSaved { get; set; }
    public int ShortHandedShotsSaved { get; set; }
    public int EvenStrengthGoalsAllowed { get; set; }
    public int PowerPlayGoalsAllowed { get; set; }
    public int ShortHandedGoalsAllowed { get; set; }
    public int TimeOnIceSeconds { get; set; }
    public bool IsStarter { get; set; }
    public POSITION Position { get; set; } = POSITION.Goalie;
}