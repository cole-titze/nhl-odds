namespace Entities.Models;

public class GameExtendedInfo
{
    public string VenueName { get; set; } = string.Empty;
    public string VenueLocation { get; set; } = string.Empty;
    public IEnumerable<TvBroadcaster> TvBroadcasters { get; set; } = new List<TvBroadcaster>();
    public string GameSummary { get; set; } = string.Empty;
    public string EventSummary { get; set; } = string.Empty;
    public string PlayByPlaySummary { get; set; } = string.Empty;
    public string FaceoffSummary { get; set; } = string.Empty;
    public string FaceoffComparisonSummary { get; set; } = string.Empty;
    public string RosterSummary { get; set; } = string.Empty;
    public string ShotSummary { get; set; } = string.Empty;
    public string ShiftChartSummary { get; set; } = string.Empty;
    public string ToiAwaySummary { get; set; } = string.Empty;
    public string ToiHomeSummary { get; set; } = string.Empty;
    public int ThreeMinuteRecapVideoId { get; set; }
    public int CondensedGameVideoId { get; set; }
}