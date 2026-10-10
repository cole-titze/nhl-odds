namespace Entities.DbModels;

/// <summary>
/// A goal's tracking replay (the PptReplayUrl on its GameGoalEvent): about 14 seconds of player and puck positions
/// leading up to the goal, at 10 frames a second. A row per fetch, including replays the NHL doesn't serve.
/// </summary>
public class DbGoalReplay
{
    public int GameId { get; set; }
    // The goal's event id (GameGoalEvent.Id)
    public int EventId { get; set; }
    // The HTTP status the replay returned (200, or 403/404 when it isn't served)
    public short HttpStatus { get; set; }
    public short FrameCount { get; set; }
    // The first frame's timeStamp (tenths of a second)
    public long? FirstTimeStamp { get; set; }
    public DateTime FetchedUTC { get; set; }
}