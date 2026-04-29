using Entities.Types;
using Microsoft.Extensions.Caching.Memory;
using WebApi.BusinessLogic.GameOddsGetter;
using WebApi.BusinessLogic.TeamGetter;

namespace WebApi.BusinessLogic.StartupCacheWarmer;

public class StartupCacheWarmer : BackgroundService
{
    private static readonly TimeZoneInfo CentralTime = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<StartupCacheWarmer> _logger;

    public StartupCacheWarmer(IServiceScopeFactory scopeFactory, IMemoryCache cache, ILogger<StartupCacheWarmer> logger)
    {
        _scopeFactory = scopeFactory;
        _cache = cache;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RefreshCache();

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayUntil7AmCentral();
            _logger.LogInformation("Next cache refresh in {Hours:F1} hours.", delay.TotalHours);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await RefreshCache();
        }
    }

    private async Task RefreshCache()
    {
        var seasonStartYear = GetCurrentSeasonStartYear();
        var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, CentralTime).Date;

        _logger.LogInformation("Refreshing cache for season {Season}...", seasonStartYear);

        using var scope = _scopeFactory.CreateScope();

        try
        {
            var teamGetter = scope.ServiceProvider.GetRequiredService<ITeamGetter>();
            var teamsVm = await teamGetter.GetAllTeamsStats(seasonStartYear);
            _cache.Set($"AllTeams_{seasonStartYear}", teamsVm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh AllTeams cache for season {Season}.", seasonStartYear);
        }

        try
        {
            var gameOddsGetter = scope.ServiceProvider.GetRequiredService<IGameOddsGetter>();

            var todayRange = new DateRange { StartDate = today, EndDate = today };
            var todayOdds = await gameOddsGetter.GetGameOddsInDateRange(todayRange, seasonStartYear);
            _cache.Set($"GameOdds_{today:yyyy-MM-dd}_{today:yyyy-MM-dd}_{seasonStartYear}", todayOdds);

            var seasonStart = new DateTime(seasonStartYear, 9, 1);
            var seasonEnd = new DateTime(seasonStartYear + 1, 7, 31);
            var seasonRange = new DateRange { StartDate = seasonStart, EndDate = seasonEnd };
            var seasonOdds = await gameOddsGetter.GetGameOddsInDateRange(seasonRange, seasonStartYear);
            _cache.Set($"GameOdds_{seasonStart:yyyy-MM-dd}_{seasonEnd:yyyy-MM-dd}_{seasonStartYear}", seasonOdds);

            var anchor = await gameOddsGetter.GetAnchorDate(seasonStartYear);
            _cache.Set($"AnchorDate_{seasonStartYear}", anchor, TimeSpan.FromMinutes(15));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh GameOdds cache for {Date}.", today);
        }

        _logger.LogInformation("Cache refreshed for season {Season}.", seasonStartYear);
    }

    // Returns the delay until the next 7am Central time.
    private static TimeSpan GetDelayUntil7AmCentral()
    {
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, CentralTime);
        var next7am = now.Date.AddHours(7);
        if (now >= next7am)
            next7am = next7am.AddDays(1);
        return next7am - now;
    }

    // NHL season starts in October — if it's before October, the season began last year.
    private static int GetCurrentSeasonStartYear()
    {
        var now = DateTime.UtcNow;
        return now.Month >= 10 ? now.Year : now.Year - 1;
    }
}