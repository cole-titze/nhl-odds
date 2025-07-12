using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.DbModels;

public class DbSeasonTeam
{
    public int TeamId { get; set; }
    public int SeasonStartYear { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Abbreviation { get; set; } = string.Empty;
    public string CommonName { get; set; } = string.Empty;
    public string LogoUri { get; set; } = string.Empty;
    public string Division { get; set; } = string.Empty;
    public string DivisionAbbreviation { get; set; } = string.Empty;
    public string Conference { get; set; } = string.Empty;
    public string ConferenceAbbreviation { get; set; } = string.Empty;
    public string PlaceName { get; set; } = string.Empty;
    [ForeignKey(nameof(TeamId))]
    public DbTeam? Team { get; set; }
    public void Clone(DbSeasonTeam team)
    {
        TeamId = team.TeamId;
        Abbreviation = team.Abbreviation;
        Name = team.Name;
        CommonName = team.CommonName;
        LogoUri = team.LogoUri;
        Division = team.Division;
        DivisionAbbreviation = team.DivisionAbbreviation;
        Conference = team.Conference;
        ConferenceAbbreviation = team.ConferenceAbbreviation;
        PlaceName = team.PlaceName;
        SeasonStartYear = team.SeasonStartYear;
        Team = team.Team;
    }
    public bool IsEquivalentTo(DbSeasonTeam? other)
    {
        if (other == null)
            return false;

        return TeamId == other.TeamId
            && SeasonStartYear == other.SeasonStartYear
            && Name == other.Name
            && Abbreviation == other.Abbreviation
            && CommonName == other.CommonName
            && LogoUri == other.LogoUri
            && Division == other.Division
            && DivisionAbbreviation == other.DivisionAbbreviation
            && Conference == other.Conference
            && ConferenceAbbreviation == other.ConferenceAbbreviation
            && PlaceName == other.PlaceName;
    }
}