namespace Entities.Types;

/// <summary>Where raw NHL API responses are kept (NhlApiResponse).</summary>
public interface IRawResponseStore
{
    /// <summary>
    /// The newest stored text for this URL, or null. For a game's URL only a copy saved after the game was over counts,
    /// so a response fetched before or during the game is never served.
    /// </summary>
    Task<string?> GetLatest(string url, int? gameId);
    /// <summary>Stores the text as a new version, or marks the same text as seen again.</summary>
    Task Save(string url, int? gameId, string body, DateTime fetchedUtc);
}