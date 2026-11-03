using Entities.DbModels;

namespace DatabaseAccess.SearchSourceRepository;

/// <summary>Reads the rows the search index is built from (RUN_MODE=SearchIndex). Read-only, untracked.</summary>
public interface ISearchSourceRepository
{
    Task<List<DbSeasonTeam>> GetSeasonTeams(CancellationToken ct = default);
    /// <summary>Every player, with the position from their latest game roster (null if they have none).</summary>
    Task<List<(DbPlayer Player, string? Position)>> GetPlayers(CancellationToken ct = default);
    Task<List<DbGameRaw>> GetGames(CancellationToken ct = default);
    /// <summary>The newest saved version of each article.</summary>
    Task<List<DbNhlArticle>> GetArticles(CancellationToken ct = default);
}
