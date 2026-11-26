namespace Entities.Models.Web;

/// <summary>A game's page in the sitemap: its URL's parts (the teams' names that season) and its start time.</summary>
public record SitemapGame(int Id, int SeasonStartYear, DateTime GameDateUTC, int AwayTeamId, string AwayTeamName,
    int HomeTeamId, string HomeTeamName, bool HasBeenPlayed);
