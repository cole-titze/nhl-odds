namespace Entities.Models
{
    public class GameExtendedInfo
    {
        public string venueName { get; set; } = string.Empty;
        public string venueLocation { get; set; } = string.Empty;
        public IEnumerable<TvBroadcaster> tvBroadcasters { get; set; } = new List<TvBroadcaster>();
        public string gameSummary { get; set; } = string.Empty;
        public string eventSummary { get; set; } = string.Empty;
        public string playByPlaySummary { get; set; } = string.Empty;
        public string faceoffSummary { get; set; } = string.Empty;
        public string faceoffComparisonSummary { get; set; } = string.Empty;
        public string rosterSummary { get; set; } = string.Empty;
        public string shotSummary { get; set; } = string.Empty;
        public string shiftChartSummary { get; set; } = string.Empty;
        public string toiAwaySummary { get; set; } = string.Empty;
        public string toiHomeSummary { get; set; } = string.Empty;
        public int threeMinuteRecapVideoId { get; set; }
        public int condensedGameVideoId { get; set; }
    }
}