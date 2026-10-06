using System.ComponentModel;
using Entities.Types;
using Entities.ViewModels;
using Microsoft.Extensions.Caching.Memory;
using ModelContextProtocol.Server;
using WebApi.BusinessLogic.GameOddsGetter;
using WebApi.Caching;

namespace WebApi.McpTools;

[McpServerToolType]
public class GameOddsMcpTools(IGameOddsGetter gameOddsGetter, [FromKeyedServices(ApiCache.ServiceKey)] IMemoryCache cache)
{
    [McpServerTool, Description("Get NHL game odds and model predictions for today.")]
    public async Task<IEnumerable<GameOddsVM>> GetTodaysGames(
        [Description("The season start year (e.g. 2024 for the 2024-25 season)")] int seasonStartYear)
    {
        var today = DateTime.UtcNow.Date;
        var dateRange = new DateRange { StartDate = today, EndDate = today };
        return await GetCachedGameOdds(dateRange, seasonStartYear);
    }

    [McpServerTool, Description("Get NHL game odds and model predictions for a date range.")]
    public async Task<IEnumerable<GameOddsVM>> GetGamesInDateRange(
        [Description("Start date (UTC)")] DateTime startDate,
        [Description("End date (UTC)")] DateTime endDate,
        [Description("The season start year (e.g. 2024 for the 2024-25 season)")] int seasonStartYear)
    {
        // Whole days, matching GameOddsController, so both share cache entries
        var dateRange = new DateRange { StartDate = startDate.Date, EndDate = endDate.Date };
        return await GetCachedGameOdds(dateRange, seasonStartYear);
    }

    private Task<IEnumerable<GameOddsVM>> GetCachedGameOdds(DateRange dateRange, int seasonStartYear) =>
        cache.GetOrSetAsync(ApiCache.GameOddsKey(dateRange, seasonStartYear),
            () => gameOddsGetter.GetGameOddsInDateRange(dateRange, seasonStartYear), r => r.Count(), ApiCache.DateRangeLifetime);
}