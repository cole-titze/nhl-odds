namespace Entities.DbModels;

/// <summary>
/// A raw NHL API response (per-game endpoints, goal replays and player pages), gzipped, so tables can be rebuilt after a
/// parser change without fetching again. A new row only when the response's text changes (NHL corrections); the same
/// text fetched again just moves LastSeenUTC.
/// </summary>
public class DbNhlApiResponse
{
    public string Url { get; set; } = string.Empty;
    // sha256 (hex) of the uncompressed text
    public string ContentHash { get; set; } = string.Empty;
    public int? GameId { get; set; }
    public DateTime FirstSeenUTC { get; set; }
    public DateTime LastSeenUTC { get; set; }
    public byte[] Body { get; set; } = [];
}