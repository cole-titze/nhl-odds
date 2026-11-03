using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Integration.Tests;

/// <summary>
/// The real Web API (Program.cs) on the test database, called over HTTP. Background services
/// (the cache warmer, the metrics server on :9091) are left out, and each test gets its own
/// host, so the in-memory API cache starts empty.
/// </summary>
[TestClass]
[TestCategory(TestDatabase.Category)]
public class ApiTests
{
    private WebApplicationFactory<Program>? _factory;
    private HttpClient _client = null!;

    [TestInitialize]
    public async Task Setup()
    {
        await TestDatabase.Reset();
        await Seed.DefaultTeams();
        // Program.cs reads the connection string from the environment before building the host
        Environment.SetEnvironmentVariable("NHL_DATABASE", TestDatabase.ConnectionString);
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                foreach (var hosted in services.Where(s => s.ServiceType == typeof(IHostedService)).ToList())
                    services.Remove(hosted);
            });
        });
        _client = _factory.CreateClient();
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        // Setup stops before the host is built when there's no test database
        _client?.Dispose();
        if (_factory is not null)
            await _factory.DisposeAsync();
    }

    [TestMethod]
    public async Task GetGame_ShouldReturnTheGameAsJson()
    {
        await Seed.Games(new Seed.Game(2026020001, new DateTime(2026, 11, 5, 0, 0, 0)));
        await Seed.Predictions(new Seed.GameOdds(2026020001, new DateTime(2026, 11, 4, 12, 0, 0), 0.58));
        await Seed.Bookmakers(new Seed.BookmakerOdds(2026020001, "DraftKings", 0.55));

        var res = await _client.GetAsync("/api/GameOdds/GetGame?gameId=2026020001");

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await res.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("id").GetInt32().Should().Be(2026020001);
        json.GetProperty("gameDate").GetString().Should().Be("2026-11-05T00:00:00Z");
        json.GetProperty("homeTeam").GetProperty("teamName").GetString().Should().Be("Maple Leafs");
        json.GetProperty("homeTeam").GetProperty("modelOdds").GetDouble().Should().BeApproximately(0.58, 1e-9);
        json.GetProperty("bookmakerOdds")[0].GetProperty("bookmakerName").GetString().Should().Be("DraftKings");
    }

    [TestMethod]
    public async Task GetGame_UnknownGame_ShouldBe404()
    {
        var res = await _client.GetAsync("/api/GameOdds/GetGame?gameId=1");

        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task GetGameOddsInDateRange_ShouldListTheDaysGames()
    {
        await Seed.Games(
            new Seed.Game(1, new DateTime(2026, 11, 5, 2, 0, 0)),
            new Seed.Game(2, new DateTime(2026, 11, 7, 2, 0, 0)));

        var games = await _client.GetFromJsonAsync<JsonElement>(
            "/api/GameOdds/GetGameOddsInDateRange?startDate=2026-11-04&endDate=2026-11-04&seasonStartYear=2026");

        games.EnumerateArray().Select(g => g.GetProperty("id").GetInt32()).Should().Equal(1);
    }

    [TestMethod]
    public async Task GetTeam_ShouldCountTheRecordFromPlayedGames()
    {
        await Seed.Games(
            new Seed.Game(1, new DateTime(2026, 10, 20, 0, 0, 0)) { Played = true, HomeGoals = 3, AwayGoals = 1, Winner = 0 },
            new Seed.Game(2, new DateTime(2026, 10, 22, 0, 0, 0)) { Played = true, HomeGoals = 1, AwayGoals = 4, Winner = 1 },
            new Seed.Game(3, new DateTime(2026, 10, 24, 0, 0, 0)) { Played = true, HomeGoals = 2, AwayGoals = 3, Winner = 1, EndPeriod = 1 },
            new Seed.Game(4, new DateTime(2026, 11, 20, 0, 0, 0)));

        var team = await _client.GetFromJsonAsync<JsonElement>("/api/Team/GetTeam?teamId=10&seasonStartYear=2026");

        team.GetProperty("teamName").GetString().Should().Be("Maple Leafs");
        team.GetProperty("seasonWins").GetInt32().Should().Be(1);
        team.GetProperty("seasonLosses").GetInt32().Should().Be(1);
        team.GetProperty("seasonOvertimeLosses").GetInt32().Should().Be(1);
        team.GetProperty("gameOddsVM").GetArrayLength().Should().Be(4);
    }

    [TestMethod]
    public async Task GetTeam_UnknownTeam_ShouldBe404()
    {
        var res = await _client.GetAsync("/api/Team/GetTeam?teamId=99&seasonStartYear=2026");

        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task GetHealthChecks_ShouldAnswerFromTheDatabase()
    {
        // Admin endpoints need X-Admin-Key outside Development; the test host sets one
        await Seed.Games(new Seed.Game(1, new DateTime(2026, 10, 20, 0, 0, 0)) { Played = true });
        _client.DefaultRequestHeaders.Add("X-Admin-Key", AdminKey);

        var checks = await _client.GetFromJsonAsync<JsonElement>("/api/Admin/GetHealthChecks");

        checks.EnumerateArray().Single().GetProperty("seasonStartYear").GetInt32().Should().Be(2026);
    }

    private const string AdminKey = "integration-test-key";

    [ClassInitialize]
    public static void SetAdminKey(TestContext _) => Environment.SetEnvironmentVariable("ADMIN_API_KEY", AdminKey);
}
