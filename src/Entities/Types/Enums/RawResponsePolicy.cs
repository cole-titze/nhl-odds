namespace Entities.Types.Enums;

/// <summary>How NHL API requests use the stored raw responses (NhlApiResponse).</summary>
public enum RawResponsePolicy
{
    /// <summary>Always ask the API (new games, corrections) and store what comes back.</summary>
    Fresh,
    /// <summary>Only stored responses for the stored endpoints, never the API (rebuilding tables after a parser change).</summary>
    StoredOnly,
}