namespace Entities.DbModels;

/// <summary>When a game's roster or shift chart was last fetched and how many rows it had (0 is a real answer: the
/// NHL has no shift charts before 2010), so the backfill can skip games already done.</summary>
public class DbGameDetailFetch
{
    public int GameId { get; set; }
    // "Roster" or "Shifts"
    public string Kind { get; set; } = string.Empty;
    public DateTime FetchedUTC { get; set; }
    public int Rows { get; set; }
}