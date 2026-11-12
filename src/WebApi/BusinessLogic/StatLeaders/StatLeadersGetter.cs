using DatabaseAccess.WebPlayerRepository;
using Entities.Models.Web;
using Entities.ViewModels;
using Microsoft.Extensions.Caching.Memory;
using WebApi.Caching;

namespace WebApi.BusinessLogic.StatLeaders;

public interface IStatLeadersGetter
{
    /// <summary>The season's leaders in the stat (see <see cref="StatLeaders.Stats"/>); null for an unknown stat.</summary>
    Task<StatLeadersVM?> GetStatLeaders(string stat, int seasonStartYear, int limit = StatLeaders.MaxLeaders);
}

/// <summary>
/// The Leaders page and the MCP tool (#182). The season's totals are read once and cached, so every stat is
/// ranked from the same rows.
/// </summary>
public class StatLeadersGetter(IWebPlayerRepository playerRepository,
    [FromKeyedServices(ApiCache.ServiceKey)] IMemoryCache cache) : IStatLeadersGetter
{
    public async Task<StatLeadersVM?> GetStatLeaders(string stat, int seasonStartYear, int limit = StatLeaders.MaxLeaders)
    {
        var leaderStat = StatLeaders.Find(stat);
        if (leaderStat is null)
            return null;
        var totals = await cache.GetOrSetAsync(ApiCache.SeasonTotalsKey(seasonStartYear),
            () => playerRepository.GetSeasonTotals(seasonStartYear), t => 1 + t.Count / 50, ApiCache.DateRangeLifetime);
        return StatLeaders.Rank(totals, leaderStat, seasonStartYear, limit);
    }
}
