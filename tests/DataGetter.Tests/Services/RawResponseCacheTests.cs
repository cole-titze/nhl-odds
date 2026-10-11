using System.Net;
using DatabaseAccess.RawResponseRepository;
using Entities.Types;
using Entities.Types.Enums;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Services.NhlData;

namespace DataGetter.Tests.Services;

[TestClass]
public class RawResponseCacheTests
{
    private const string URL = "https://api-web.nhle.com/v1/gamecenter/2023020204/boxscore";

    private static (RawResponseCache Cache, IRawResponseStore Store) Cache(RawResponsePolicy policy, string? stored)
    {
        var store = A.Fake<IRawResponseStore>();
        A.CallTo(() => store.GetLatest(A<string>._, A<int?>._)).Returns(stored);
        return (new RawResponseCache(store, policy, NullLoggerFactory.Instance), store);
    }

    private static Func<Task<(HttpStatusCode, string?)>> Api(string body, Action? called = null) => () =>
    {
        called?.Invoke();
        return Task.FromResult<(HttpStatusCode, string?)>((HttpStatusCode.OK, body));
    };

    [TestMethod]
    public async Task Fresh_AsksTheApiAndStores()
    {
        var (cache, store) = Cache(RawResponsePolicy.Fresh, "old");

        var (_, body) = await cache.Get(URL, Api("new"));

        body.Should().Be("new");
        A.CallTo(() => store.GetLatest(A<string>._, A<int?>._)).MustNotHaveHappened();
        A.CallTo(() => store.Save(URL, 2023020204, "new", A<DateTime>._)).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task PreferStored_UsesTheStoredCopyElseTheApi()
    {
        var calls = 0;
        var (cache, _) = Cache(RawResponsePolicy.PreferStored, "stored");
        (await cache.Get(URL, Api("api", () => calls++))).Body.Should().Be("stored");
        calls.Should().Be(0);

        var (empty, store) = Cache(RawResponsePolicy.PreferStored, null);
        (await empty.Get(URL, Api("api", () => calls++))).Body.Should().Be("api");
        calls.Should().Be(1);
        A.CallTo(() => store.Save(URL, 2023020204, "api", A<DateTime>._)).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task StoredOnly_NeverAsksTheApiForAStoredEndpoint()
    {
        var (cache, _) = Cache(RawResponsePolicy.StoredOnly, null);

        var act = () => cache.Get(URL, Api("api", () => Assert.Fail("asked the API")));

        await act.Should().ThrowAsync<StoredResponseMissingException>();
        // Endpoints that aren't stored (standings, current rosters) still go to the API
        (await cache.Get("https://api-web.nhle.com/v1/standings/now", Api("standings"))).Body.Should().Be("standings");
    }

    [TestMethod]
    public async Task StoreFailures_DontStopTheRequest()
    {
        var store = A.Fake<IRawResponseStore>();
        A.CallTo(() => store.GetLatest(A<string>._, A<int?>._)).ThrowsAsync(new Exception("no table"));
        A.CallTo(() => store.Save(A<string>._, A<int?>._, A<string>._, A<DateTime>._)).ThrowsAsync(new Exception("no table"));
        var cache = new RawResponseCache(store, RawResponsePolicy.PreferStored, NullLoggerFactory.Instance);

        (await cache.Get(URL, Api("api"))).Body.Should().Be("api");
    }

    [TestMethod]
    public async Task OnlySuccessfulResponsesAreStored()
    {
        var (cache, store) = Cache(RawResponsePolicy.Fresh, null);

        await cache.Get(URL, () => Task.FromResult<(HttpStatusCode, string?)>((HttpStatusCode.NotFound, null)));

        A.CallTo(() => store.Save(A<string>._, A<int?>._, A<string>._, A<DateTime>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public void GameIdOf_ReadsEveryGameUrlShape()
    {
        RawResponseCache.GameIdOf(URL).Should().Be(2023020204);
        RawResponseCache.GameIdOf(NhlGameDetailGetter.ShiftChartUrl(2010020500)).Should().Be(2010020500);
        RawResponseCache.GameIdOf("https://wsr.nhle.com/sprites/20262027/2026020069/ev976.json").Should().Be(2026020069);
        RawResponseCache.GameIdOf("https://api-web.nhle.com/v1/player/8478402/landing").Should().BeNull();
    }

    [TestMethod]
    public void Gzip_RoundTrips()
    {
        var body = """{"id":2023020204,"name":"Stützle ✓"}""";

        RawResponseRepository.Decompress(RawResponseRepository.Compress(body)).Should().Be(body);
        RawResponseRepository.Hash(body).Should().HaveLength(64);
    }
}