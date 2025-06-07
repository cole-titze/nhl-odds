namespace Entities.Models
{
    /// <summary>
    /// Represents a referee in the NHL.
    /// Currently only contains the name, but may expand in the future.
    /// </summary>
    public class Referee
    {
        public string name { get; set; } = string.Empty;
    }
}