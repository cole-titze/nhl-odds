using Entities.Types;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using WebApi.BusinessLogic.GameOddsGetter;
using WebApi.Caching;

namespace WebApi.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public class GameOddsController
{
    private readonly IGameOddsGetter _gameOddsGetter;
    private readonly IMemoryCache _cache;

    public GameOddsController(IGameOddsGetter predictedGameBL, [FromKeyedServices(ApiCache.ServiceKey)] IMemoryCache cache)
    {
        _gameOddsGetter = predictedGameBL;
        _cache = cache;
    }

    [HttpGet]
    public async Task<IResult> GetGameOddsInDateRange(DateTime startDate, DateTime endDate, int seasonStartYear)
    {
        var dateRange = new DateRange
        {
            StartDate = startDate.Date,
            EndDate = endDate.Date
        };

        var result = await _cache.GetOrSetAsync(ApiCache.GameOddsKey(dateRange, seasonStartYear),
            () => _gameOddsGetter.GetGameOddsInDateRange(dateRange, seasonStartYear), r => r.Count(), ApiCache.DateRangeLifetime);
        return Results.Ok(result);
    }

    [HttpGet]
    public async Task<IResult> GetAnchorDate(int seasonStartYear)
    {
        var result = await _cache.GetOrSetAsync(ApiCache.AnchorDateKey(seasonStartYear),
            () => _gameOddsGetter.GetAnchorDate(seasonStartYear), _ => 1, TimeSpan.FromMinutes(15));
        return Results.Ok(result);
    }
}