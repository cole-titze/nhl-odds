namespace Entities.DbModels;

public class DbTeam
{
    public int Id { get; set; }
    public string Abbreviation { get; set; } = string.Empty;
    public int FranchiseId { get; set; }
    public int LeagueId { get; set; }
    public void Clone(DbTeam team)
    {
        Id = team.Id;
        Abbreviation = team.Abbreviation;
        FranchiseId = team.FranchiseId;
        LeagueId = team.LeagueId;
    }
    public bool IsEquivalentTo(DbTeam? other)
    {
        if (other == null)
            return false;

        return Id == other.Id
            && Abbreviation == other.Abbreviation
            && FranchiseId == other.FranchiseId
            && LeagueId == other.LeagueId;
    }
}