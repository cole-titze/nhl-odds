using DatabaseAccess.WebLineupRepository;
using Entities.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using WebApi.Caching;
using WebApi.Mappers;

namespace WebApi.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public class LineupController(IWebLineupRepository repository, [FromKeyedServices(ApiCache.ServiceKey)] IMemoryCache cache)
{
    // LineupSnapshot saves a new version every 30 minutes on game days, so keep it fresh
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);

    // 200 with a null lineup when there isn't one yet, so most game pages don't log a 404
    [HttpGet]
    public async Task<IResult> GetGameLineup(int gameId)
    {
        var result = await cache.GetOrSetAsync(ApiCache.LineupKey(gameId), async () =>
        {
            var lineup = await repository.GetGameLineup(gameId);
            return new GameLineupResultVM { Lineup = lineup == null ? null : GameLineupToGameLineupVmMapper.Map(lineup) };
        }, r => r.Lineup == null ? 1 : 1 + r.Lineup.Away.Forwards.Count + r.Lineup.Home.Forwards.Count, Lifetime);
        return Results.Ok(result);
    }
}
