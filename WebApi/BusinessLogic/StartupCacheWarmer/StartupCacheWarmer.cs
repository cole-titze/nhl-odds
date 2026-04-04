using Microsoft.Extensions.Caching.Memory;
using WebApi.BusinessLogic.TeamGetter;

namespace WebApi.BusinessLogic.StartupCacheWarmer;

public class StartupCacheWarmer : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<StartupCacheWarmer> _logger;

    public StartupCacheWarmer(IServiceScopeFactory scopeFactory, IMemoryCache cache, ILogger<StartupCacheWarmer> logger)
    {
        _scopeFactory = scopeFactory;
        _cache = cache;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var seasonStartYear = GetCurrentSeasonStartYear();
        var cacheKey = $"AllTeams_{seasonStartYear}";

        _logger.LogInformation("Warming cache for season {Season}...", seasonStartYear);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var teamGetter = scope.ServiceProvider.GetRequiredService<ITeamGetter>();
            var teamsVm = await teamGetter.GetAllTeamsStats(seasonStartYear);
            _cache.Set(cacheKey, teamsVm, TimeSpan.FromMinutes(5));
            _logger.LogInformation("Cache warmed for season {Season}.", seasonStartYear);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to warm cache for season {Season}.", seasonStartYear);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    // NHL season starts in October — if it's before October, the season began last year.
    private static int GetCurrentSeasonStartYear()
    {
        var now = DateTime.UtcNow;
        return now.Month >= 10 ? now.Year : now.Year - 1;
    }
}
