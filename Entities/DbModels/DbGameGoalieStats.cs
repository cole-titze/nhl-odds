using System.ComponentModel.DataAnnotations.Schema;

using Entities.Types;

namespace Entities.DbModels;

public class DbGameGoalieStats : IDbGamePlayerStats
{
    public int GameId { get; set; }
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
    [ForeignKey(nameof(PlayerId))]
    public DbPlayer? Player { get; set; }
    [ForeignKey(nameof(GameId))]
    public DbGameRaw? Game { get; set; }
    [ForeignKey(nameof(TeamId))]
    public DbTeam? Team { get; set; }
    public void Clone(IDbGamePlayerStats gamePlayerStats)
    {
        if (gamePlayerStats is DbGameGoalieStats goalieStats)
        {
            GameId = goalieStats.GameId;
            TeamId = goalieStats.TeamId;
            PlayerId = goalieStats.PlayerId;
            EvenStrengthShotsSaved = goalieStats.EvenStrengthShotsSaved;
            PowerPlayShotsSaved = goalieStats.PowerPlayShotsSaved;
            EvenStrengthGoalsAllowed = goalieStats.EvenStrengthGoalsAllowed;
            PowerPlayGoalsAllowed = goalieStats.PowerPlayGoalsAllowed;
            TimeOnIceSeconds = goalieStats.TimeOnIceSeconds;
            IsStarter = goalieStats.IsStarter;
            Position = goalieStats.Position;
            Player = goalieStats.Player;
            Game = goalieStats.Game;
            Team = goalieStats.Team;
        }
        else
        {
            throw new InvalidOperationException("Invalid type passed to CloneFrom.");
        }
    }
    public bool IsEquivalentTo(IDbGamePlayerStats? other)
    {
        if (other == null || other is not DbGameGoalieStats goalieStats)
            return false;

        return GameId == goalieStats.GameId
            && PlayerId == goalieStats.PlayerId
            && TeamId == goalieStats.TeamId
            && EvenStrengthShotsSaved == goalieStats.EvenStrengthShotsSaved
            && PowerPlayShotsSaved == goalieStats.PowerPlayShotsSaved
            && ShortHandedShotsSaved == goalieStats.ShortHandedShotsSaved
            && EvenStrengthGoalsAllowed == goalieStats.EvenStrengthGoalsAllowed
            && PowerPlayGoalsAllowed == goalieStats.PowerPlayGoalsAllowed
            && ShortHandedGoalsAllowed == goalieStats.ShortHandedGoalsAllowed
            && TimeOnIceSeconds == goalieStats.TimeOnIceSeconds
            && IsStarter == goalieStats.IsStarter
            && Position == goalieStats.Position;

    }
}
