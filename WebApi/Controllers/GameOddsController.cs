using Entities.Types;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using WebApi.BusinessLogic.GameOddsGetter;

namespace WebApi.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public class GameOddsController
{
    private readonly IGameOddsGetter _gameOddsGetter;
    private readonly IMemoryCache _cache;

    public GameOddsController(IGameOddsGetter predictedGameBL, IMemoryCache cache)
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

        var cacheKey = $"GameOdds_{dateRange.StartDate:yyyy-MM-dd}_{dateRange.EndDate:yyyy-MM-dd}_{seasonStartYear}";
        if (_cache.TryGetValue(cacheKey, out object? cached))
            return Results.Ok(cached);

        var result = await _gameOddsGetter.GetGameOddsInDateRange(dateRange, seasonStartYear);
        _cache.Set(cacheKey, result);
        return Results.Ok(result);
    }

    [HttpGet]
    public async Task<IResult> GetAnchorDate(int seasonStartYear)
    {
        var cacheKey = $"AnchorDate_{seasonStartYear}";
        if (_cache.TryGetValue(cacheKey, out object? cached))
            return Results.Ok(cached);

        var result = await _gameOddsGetter.GetAnchorDate(seasonStartYear);
        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(15));
        return Results.Ok(result);
    }
}
