namespace Entities.Models
{
    /// <summary>
    /// I don't know what any of this, but could be useful in the future.
    /// </summary>
    public class TvBroadcaster
    {
        public int id { get; set; }
        public string name { get; set; } = string.Empty;
        public string marketAbbreviation { get; set; } = string.Empty;
        public string network { get; set; } = string.Empty;
        public string sequenceNumber { get; set; } = string.Empty;
    }
}