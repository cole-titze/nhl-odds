using DatabaseAccess.WebPredictionFactorRepository;
using Entities.Types;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using WebApi.BusinessLogic.GameOddsGetter;
using WebApi.Caching;
using WebApi.Mappers;

namespace WebApi.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public class GameOddsController
{
    private readonly IGameOddsGetter _gameOddsGetter;
    private readonly IWebPredictionFactorRepository _factorRepository;
    private readonly IMemoryCache _cache;

    public GameOddsController(IGameOddsGetter predictedGameBL, IWebPredictionFactorRepository factorRepository,
        [FromKeyedServices(ApiCache.ServiceKey)] IMemoryCache cache)
    {
        _gameOddsGetter = predictedGameBL;
        _factorRepository = factorRepository;
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

    // One game, for the Game page; 404 if there's no such game
    [HttpGet]
    public async Task<IResult> GetGame(int gameId)
    {
        var result = await _cache.GetOrSetAsync(ApiCache.GameKey(gameId),
            () => _gameOddsGetter.GetGameOdds(gameId), _ => 1, ApiCache.DateRangeLifetime);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    // The main factors behind the game's prediction, for the Game page; factors: null when the
    // prediction shown has none (saved before #155, or the explainer failed that run)
    [HttpGet]
    public async Task<IResult> GetPredictionFactors(int gameId)
    {
        var result = await _cache.GetOrSetAsync(ApiCache.PredictionFactorsKey(gameId),
            async () => await _factorRepository.GetForLatestPrediction(gameId) is { } f
                ? PredictionFactorsToVmMapper.Map(f)
                : null,
            _ => 1, ApiCache.DateRangeLifetime);
        return Results.Ok(new { factors = result });
    }

    [HttpGet]
    public async Task<IResult> GetAnchorDate(int seasonStartYear)
    {
        var result = await _cache.GetOrSetAsync(ApiCache.AnchorDateKey(seasonStartYear),
            () => _gameOddsGetter.GetAnchorDate(seasonStartYear), _ => 1, TimeSpan.FromMinutes(15));
        return Results.Ok(result);
    }
}