namespace Entities.DbModels;

/// <summary>
/// One row of a game's shift chart: a player's shift on the ice (TypeCode 517), or a goal marker (TypeCode 505) that
/// the chart lists between shifts. Times are seconds into the period.
/// </summary>
public class DbGameShift
{
    // The NHL's id for the row
    public long Id { get; set; }
    public int GameId { get; set; }
    public int PlayerId { get; set; }
    public int TeamId { get; set; }
    public short Period { get; set; }
    public short ShiftNumber { get; set; }
    public short StartSeconds { get; set; }
    public short EndSeconds { get; set; }
    public short? DurationSeconds { get; set; }
    public short TypeCode { get; set; }
    public short DetailCode { get; set; }
    public int? EventNumber { get; set; }
    // For goal markers: "EVG", "PPG", "SHG", ...
    public string? EventDescription { get; set; }
    public string? EventDetails { get; set; }
}