using Entities.Types;

namespace Entities.ViewModels;

public class MatchupTeamVM
{
    public int id { get; set; }
    public string locationName { get; set; } = string.Empty;
    public string teamName { get; set; } = string.Empty;
    public string logoUri { get; set; } = string.Empty;
    public double modelOdds { get; set; }
    public int goals { get; set; }
    public Winner team { get; set; }
}
