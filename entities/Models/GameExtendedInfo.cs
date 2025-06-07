namespace Entities.Models
{
    public class GameExtendedInfo
    {
        public string venueName { get; set; } = string.Empty;
        public string venueLocation { get; set; } = string.Empty;
        public IEnumerable<TvBroadcaster> tvBroadcasters { get; set; } = new List<TvBroadcaster>();
    }
}