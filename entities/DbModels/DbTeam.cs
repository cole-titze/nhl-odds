namespace Entities.DbModels;

public class DbTeam
{
    public int Id { get; set; }
    public string Abbreviation { get; set; } = string.Empty;
    public void Clone(DbTeam team)
    {
        Id = team.Id;
        Abbreviation = team.Abbreviation;
    }
}