using Entities.ViewModels;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using WebApi;
using WebApi.Caching;

namespace WebApi.Tests.BusinessLogic.UnitTests.CacheTests;

[TestClass]
public class ApiCacheTests
{
    private static readonly int CurrentSeason = Seasons.Current(DateTime.UtcNow);

    [TestMethod]
    public async Task GetOrSetAllTeamsAsync_WhenCacheCompacts_ShouldKeepOnlyCurrentSeason()
    {
        var cache = (MemoryCache)ApiCache.Create();
        await cache.GetOrSetAllTeamsAsync(CurrentSeason, () => Task.FromResult(new TeamsVM()));
        await cache.GetOrSetAllTeamsAsync(CurrentSeason - 1, () => Task.FromResult(new TeamsVM()));
        cache.Set("Player_1", new PlayerVM(), ApiCache.Entry(1));

        // What the cache does when it's full, for every entry it's allowed to remove
        cache.Compact(1.0);

        cache.TryGetValue(ApiCache.AllTeamsKey(CurrentSeason), out _).Should().BeTrue();
        cache.TryGetValue(ApiCache.AllTeamsKey(CurrentSeason - 1), out _).Should().BeFalse();
        cache.TryGetValue("Player_1", out _).Should().BeFalse();
    }
}
