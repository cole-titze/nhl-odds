using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using WebApi.BusinessLogic.PlayerGetter;
using WebApi.Caching;

namespace WebApi.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public class PlayerController
{
    private readonly IPlayerGetter _playerGetter;
    private readonly IMemoryCache _cache;

    public PlayerController(IPlayerGetter playerGetter, [FromKeyedServices(ApiCache.ServiceKey)] IMemoryCache cache)
    {
        _playerGetter = playerGetter;
        _cache = cache;
    }

    // The Player page: bio, current team and the season's games; 404 if there's no such player
    [HttpGet]
    public async Task<IResult> GetPlayer(int playerId, int seasonStartYear)
    {
        var result = await _cache.GetOrSetAsync(ApiCache.PlayerKey(playerId, seasonStartYear),
            () => _playerGetter.GetPlayer(playerId, seasonStartYear), ApiCache.SizeOf, ApiCache.DateRangeLifetime);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    // The players whose latest game in the season was for the team (the Team page's roster)
    [HttpGet]
    public async Task<IResult> GetTeamRoster(int teamId, int seasonStartYear)
    {
        var result = await _cache.GetOrSetAsync(ApiCache.TeamRosterKey(teamId, seasonStartYear),
            () => _playerGetter.GetTeamRoster(teamId, seasonStartYear), r => r.Count, ApiCache.DateRangeLifetime);
        return Results.Ok(result);
    }

    // The Game page's players: the box score once played, else both rosters; 404 if there's no such game
    [HttpGet]
    public async Task<IResult> GetGamePlayers(int gameId)
    {
        var result = await _cache.GetOrSetAsync(ApiCache.GamePlayersKey(gameId),
            () => _playerGetter.GetGamePlayers(gameId), ApiCache.SizeOf, ApiCache.DateRangeLifetime);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    // Everyone with a game in the season and the date of his last one, for /sitemap.xml
    [HttpGet]
    public async Task<IResult> GetActivePlayers(int seasonStartYear)
    {
        var result = await _cache.GetOrSetAsync(ApiCache.ActivePlayersKey(seasonStartYear),
            () => _playerGetter.GetActivePlayers(seasonStartYear), r => r.Count / 50, ApiCache.DateRangeLifetime);
        return Results.Ok(result);
    }
}
