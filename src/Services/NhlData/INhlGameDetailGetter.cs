using System.Net;

namespace Services.NhlData;

/// <summary>Fetches per-game detail the main game fetch doesn't keep: rosters, shift charts and goal tracking replays.</summary>
public interface INhlGameDetailGetter
{
    /// <summary>The game's play-by-play (for its rosterSpots). Throws on failure.</summary>
    Task<string> GetPlayByPlay(int gameId);
    /// <summary>The game's shift chart. Throws on failure.</summary>
    Task<string> GetShiftChart(int gameId);
    /// <summary>A goal's tracking replay (its stored PptReplayUrl): the status, and the body when it's 200.</summary>
    Task<(HttpStatusCode Status, string? Body)> GetGoalReplay(string url);
}