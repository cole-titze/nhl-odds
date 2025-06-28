namespace Entities.Models;

/// <summary>
/// I don't know what any of this, but could be useful in the future.
/// </summary>
public class TvBroadcaster
{
    public int Id { get; set; }
    public string NetworkName { get; set; } = string.Empty;
    public string MarketAbbreviation { get; set; } = string.Empty;
    public int SequenceNumber { get; set; }
    public string CountryCode { get; set; } = string.Empty;
}