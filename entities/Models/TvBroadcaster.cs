namespace Entities.Models
{
    /// <summary>
    /// I don't know what any of this, but could be useful in the future.
    /// </summary>
    public class TvBroadcaster
    {
        public int id { get; set; }
        public string networkName { get; set; } = string.Empty;
        public string marketAbbreviation { get; set; } = string.Empty;
        public int sequenceNumber { get; set; }
        public string countryCode { get; set; } = string.Empty;
    }
}