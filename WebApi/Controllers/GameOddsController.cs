using Entities.Types;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using WebApi.BusinessLogic.GameOddsGetter;

namespace WebApi.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public class GameOddsController
{
    private static readonly TimeZoneInfo CentralTime = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");

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

        var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, CentralTime).Date;
        var isToday = dateRange.StartDate == today && dateRange.EndDate == today;

        if (isToday)
        {
            var cacheKey = $"GameOdds_{today:yyyy-MM-dd}_{seasonStartYear}";
            if (_cache.TryGetValue(cacheKey, out object? cached))
                return Results.Ok(cached);

            var result = await _gameOddsGetter.GetGameOddsInDateRange(dateRange, seasonStartYear);
            _cache.Set(cacheKey, result);
            return Results.Ok(result);
        }

        var predictedGamesVM = await _gameOddsGetter.GetGameOddsInDateRange(dateRange, seasonStartYear);
        return Results.Ok(predictedGamesVM);
    }
}
