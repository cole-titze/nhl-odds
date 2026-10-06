using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using WebApi.BusinessLogic.StrategyBacktester;
using WebApi.Caching;

namespace WebApi.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public class StrategyController
{
    private readonly IStrategyBacktester _backtester;
    private readonly IMemoryCache _cache;

    public StrategyController(IStrategyBacktester backtester, [FromKeyedServices(ApiCache.ServiceKey)] IMemoryCache cache)
    {
        _backtester = backtester;
        _cache = cache;
    }

    [HttpGet]
    public async Task<IResult> GetBestStrategies(int seasonStartYear)
    {
        var cacheKey = $"BestStrategies_{seasonStartYear}";
        if (_cache.TryGetValue(cacheKey, out object? cached))
            return Results.Ok(cached);

        var result = await _backtester.GetBestStrategies(seasonStartYear);
        _cache.Set(cacheKey, result, ApiCache.Entry(result.Count(), TimeSpan.FromDays(1)));
        return Results.Ok(result);
    }

}
