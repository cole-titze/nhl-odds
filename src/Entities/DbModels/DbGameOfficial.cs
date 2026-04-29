using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.DbModels;

public enum Role
{
    Referee,
    Linesman,
}
public class DbGameOfficial
{
    public int GameId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Role Role { get; set; } = Role.Referee;
    [ForeignKey(nameof(GameId))]
    public DbGameRaw? Game { get; set; }

    public void Clone(DbGameOfficial gameOfficial)
    {
        GameId = gameOfficial.GameId;
        Name = gameOfficial.Name;
        Role = gameOfficial.Role;
    }

    public bool IsEquivalentTo(DbGameOfficial? other)
    {
        if (other == null)
            return false;

        return GameId == other.GameId
            && Name == other.Name
            && Role == other.Role;
    }
}