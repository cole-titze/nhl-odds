namespace Services.NhlData;

/// <summary>Fetches NHL.com content that only shows its current state (articles, the odds widget). Throws on failure.</summary>
public interface INhlContentGetter
{
    /// <summary>The newest articles across NHL.com and the team sites (list items have no body).</summary>
    Task<string> GetLatestStories(int limit);
    /// <summary>One article with its body, by its selfUrl.</summary>
    Task<string> GetStoryByUrl(string selfUrl);
    /// <summary>One article with its body, by its slug.</summary>
    Task<string> GetStoryBySlug(string slug);
    /// <summary>The betting-partner odds widget for a country ("US" or "CA").</summary>
    Task<string> GetPartnerOdds(string country);
}