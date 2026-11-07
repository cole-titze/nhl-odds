namespace Entities.Models.Web;

/// <summary>A player's id and name, for looking players up by name.</summary>
public class PlayerName
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}
