namespace Entities.Models.Teams;

public class SeasonTeam
{
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
}