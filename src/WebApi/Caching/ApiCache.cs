using Entities.ViewModels;
using Microsoft.Extensions.Caching.Memory;

namespace WebApi.Caching;

/// <summary>
/// Size-limited cache for API responses. Cache keys come from query parameters, so without a limit
/// anyone could fill memory by requesting many distinct date ranges or seasons.
/// Each entry's Size is roughly its row count, so the limit bounds memory rather than entry count.
/// </summary>
public static class ApiCache
{
    public const string ServiceKey = "api";

    // ~6KB per unit (measured 2026-10-04): a full 20k cache took the pod to ~214Mi of its 256Mi limit.
    // 10k (~60Mi) still fits the warmed current season (~4.2k) plus a few more seasons.
    public const long SizeLimit = 10_000;

    // Ad-hoc date ranges aren't refreshed by the StartupCacheWarmer, so let them expire
    public static readonly TimeSpan DateRangeLifetime = TimeSpan.FromHours(1);

    public static IMemoryCache Create() => new MemoryCache(new MemoryCacheOptions { SizeLimit = SizeLimit });

    public static MemoryCacheEntryOptions Entry(long size, TimeSpan? lifetime = null) => new()
    {
        Size = Math.Max(1, size),
        AbsoluteExpirationRelativeToNow = lifetime,
    };

    public static long SizeOf(TeamsVM teams) => teams.Teams.Sum(t => 1 + t.GameOddsVM.Count());
}
