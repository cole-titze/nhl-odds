namespace Entities.Models.Web;

public class Team
{
    public int Id { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public string LogoUri { get; set; } = string.Empty;
}
