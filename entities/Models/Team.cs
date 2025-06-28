namespace Entities.Models;

public class Team
{
    public int Id { get; set; }
    public string Abbreviation { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public string LogoUri { get; set; } = string.Empty;

}

