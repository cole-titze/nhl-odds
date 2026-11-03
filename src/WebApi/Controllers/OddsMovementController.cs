using DatabaseAccess.WebOddsMovementRepository;
using Entities.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using WebApi.BusinessLogic.OddsMovement;
using WebApi.Caching;

namespace WebApi.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public class OddsMovementController(IWebOddsMovementRepository repository, [FromKeyedServices(ApiCache.ServiceKey)] IMemoryCache cache)
{
    // New widget versions arrive every 30 minutes and closing odds every 5 on game days
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);

    // 404 only for an unknown game; a game with no prices yet has no series
    [HttpGet]
    public async Task<IResult> GetOddsMovement(int gameId)
    {
        var key = ApiCache.OddsMovementKey(gameId);
        if (cache.TryGetValue(key, out OddsMovementVM? cached) && cached != null)
            return Results.Ok(cached);

        var game = await repository.GetOddsMovement(gameId);
        if (game == null)
            return Results.NotFound();
        var result = OddsMovementCalculator.Calculate(game);
        cache.Set(key, result, ApiCache.Entry(1 + result.Series.Sum(s => s.Points.Count) / 10, Lifetime));
        return Results.Ok(result);
    }
}
