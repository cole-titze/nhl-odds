namespace Entities.Models.Teams;

public class Team
{
    public int Id { get; set; }
    public string Abbreviation { get; set; } = string.Empty;
    public int FranchiseId { get; set; }
    public int LeagueId { get; set; }
    public IDictionary<int, SeasonTeam> SeasonInformation { get; set; } = new Dictionary<int, SeasonTeam>();
}