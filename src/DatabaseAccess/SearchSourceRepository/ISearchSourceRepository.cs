using Entities.DbModels;

namespace DatabaseAccess.SearchSourceRepository;

/// <summary>Reads the rows the search index is built from (RUN_MODE=SearchIndex). Read-only, untracked.</summary>
public interface ISearchSourceRepository
{
    Task<List<DbSeasonTeam>> GetSeasonTeams(CancellationToken ct = default);
    /// <summary>
    /// Every player, with the position from their latest game roster (null if they have none) and their current
    /// team: the team listing them on the NHL's latest roster report (RosterStatus) when there's one from the last
    /// <see cref="SearchSourceRepository.RosterReportMaxAge"/> (not on it: no team), else the team of their latest
    /// game if it was this season. Player.CurrentTeamId isn't used: it's only set when a player is first fetched.
    /// </summary>
    Task<List<(DbPlayer Player, string? Position, int? CurrentTeamId)>> GetPlayers(DateTime nowUtc, CancellationToken ct = default);
    Task<List<DbGameRaw>> GetGames(CancellationToken ct = default);
    /// <summary>The newest saved version of each article.</summary>
    Task<List<DbNhlArticle>> GetArticles(CancellationToken ct = default);
}
