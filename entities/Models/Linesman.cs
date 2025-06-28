namespace Entities.Models;

/// <summary>
/// Represents a linesman in the NHL.
/// Currently only contains the name, but may expand in the future.
/// </summary>
public class Linesman : IOfficial
{
    public string Name { get; set; } = string.Empty;
}