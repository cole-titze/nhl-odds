using Entities.Types;
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
    public static long SizeOf(TeamVM? team) => team is null ? 1 : 1 + team.GameOddsVM.Count();
    public static long SizeOf(SeasonTeamsVM seasonTeams) => seasonTeams.Teams.Count() + seasonTeams.Games.Count();
    public static long SizeOf(PlayerVM? player) => 1 + (player?.Games.Count() ?? 0);
    public static long SizeOf(GamePlayersVM? players) => 1 + (players == null ? 0 : players.Home.Count() + players.Away.Count());

    // Keys shared by the controllers, MCP tools and StartupCacheWarmer so they all hit the same entries
    public static string AllTeamsKey(int seasonStartYear) => $"AllTeams_{seasonStartYear}";
    public static string SeasonTeamsKey(int seasonStartYear) => $"SeasonTeams_{seasonStartYear}";
    public static string TeamKey(int teamId, int seasonStartYear) => $"Team_{teamId}_{seasonStartYear}";
    public static string GameOddsKey(DateRange dateRange, int seasonStartYear) =>
        $"GameOdds_{dateRange.StartDate:yyyy-MM-dd}_{dateRange.EndDate:yyyy-MM-dd}_{seasonStartYear}";
    public static string AnchorDateKey(int seasonStartYear) => $"AnchorDate_{seasonStartYear}";
    public static string GameKey(int gameId) => $"Game_{gameId}";
    public static string LineupKey(int gameId) => $"Lineup_{gameId}";
    public static string OddsMovementKey(int gameId) => $"OddsMovement_{gameId}";
    public static string TrackRecordKey(int seasonStartYear, bool liveOnly) => $"TrackRecord_{seasonStartYear}_{liveOnly}";
    public static string PredictionFactorsKey(int gameId) => $"PredictionFactors_{gameId}";
    public static string PlayerKey(int playerId, int seasonStartYear) => $"Player_{playerId}_{seasonStartYear}";
    public static string TeamRosterKey(int teamId, int seasonStartYear) => $"TeamRoster_{teamId}_{seasonStartYear}";
    public static string GamePlayersKey(int gameId) => $"GamePlayers_{gameId}";
    public static string ActivePlayersKey(int seasonStartYear) => $"ActivePlayers_{seasonStartYear}";

    public static async Task<T> GetOrSetAsync<T>(this IMemoryCache cache, string key, Func<Task<T>> create,
        Func<T, long> sizeOf, TimeSpan? lifetime = null)
    {
        if (cache.TryGetValue(key, out T? cached) && cached is not null)
            return cached;

        var value = await create();
        cache.Set(key, value, Entry(sizeOf(value), lifetime));
        return value;
    }
}
