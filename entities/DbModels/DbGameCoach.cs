using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.DbModels;

public class DbGameCoach
{
    public int GameId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TeamId { get; set; }
    [ForeignKey(nameof(GameId))]
    public DbGameRaw? Game { get; set; }
    [ForeignKey(nameof(TeamId))]
    public DbTeam? Team { get; set; }

    public void Clone(DbGameCoach coach)
    {
        GameId = coach.GameId;
        Name = coach.Name;
        TeamId = coach.TeamId;
        Game = coach.Game;
        Team = coach.Team;
    }

    public bool IsEquivalentTo(DbGameCoach? other)
    {
        if (other == null)
            return false;

        return GameId == other.GameId &&
               Name == other.Name &&
               TeamId == other.TeamId;
    }
}