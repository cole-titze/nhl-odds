namespace Entities.DbModels;

/// <summary>
/// Where one tracked object (a player or the puck) was in one frame of a goal's tracking replay. The x/y are the
/// replay's own rink pixels (about 0-2400 by 0-1020), not the play-by-play's feet-from-center-ice coordinates.
/// </summary>
public class DbGoalReplayPosition
{
    public int GameId { get; set; }
    public int EventId { get; set; }
    // 0-based frame index; frames are a tenth of a second apart
    public short Frame { get; set; }
    // The replay's id for the object: 1 is the puck, players are teamId × 1000 + sweater number
    public int TrackId { get; set; }
    // The frame's timeStamp (tenths of a second)
    public long TimeStamp { get; set; }
    // Null for the puck
    public int? PlayerId { get; set; }
    public int? TeamId { get; set; }
    public short? SweaterNumber { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
}