using Entities.Types;
using Entities.ViewModels;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using WebApi.BusinessLogic.GameOddsGetter;
using WebApi.BusinessLogic.TeamGetter;
using WebApi.Caching;
using WebApi.Controllers;
using WebApi.McpTools;

namespace WebApi.Tests.BusinessLogic.UnitTests.McpToolsTests;

[TestClass]
public class McpToolsCacheTests
{
    private const int YEAR = 2026;

    private ITeamGetter _teamGetter = null!;
    private IGameOddsGetter _gameOddsGetter = null!;
    private ServiceProvider _services = null!;

    [TestInitialize]
    public void Setup()
    {
        _teamGetter = A.Fake<ITeamGetter>();
        _gameOddsGetter = A.Fake<IGameOddsGetter>();
        A.CallTo(() => _teamGetter.GetAllTeamsStats(A<int>._)).ReturnsLazily(() => new TeamsVM());
        A.CallTo(() => _teamGetter.GetTeamStats(A<int>._, A<int>._)).ReturnsLazily(() => new TeamVM());
        A.CallTo(() => _gameOddsGetter.GetGameOddsInDateRange(A<DateRange>._, A<int>._))
            .ReturnsLazily(() => new List<GameOddsVM> { new() });

        // Build the tools from DI so the keyed API cache is injected
        _services = new ServiceCollection()
            .AddSingleton(_teamGetter)
            .AddSingleton(_gameOddsGetter)
            .AddKeyedSingleton(ApiCache.ServiceKey, (_, _) => ApiCache.Create())
            .BuildServiceProvider();
    }

    [TestCleanup]
    public void Cleanup() => _services.Dispose();

    private T Create<T>() => ActivatorUtilities.CreateInstance<T>(_services);

    [TestMethod]
    public async Task GetAllTeams_CalledTwice_ShouldBuildOnce()
    {
        var cut = Create<TeamMcpTools>();

        await cut.GetAllTeams(YEAR);
        await cut.GetAllTeams(YEAR);

        A.CallTo(() => _teamGetter.GetAllTeamsStats(YEAR)).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task GetAllTeams_AfterController_ShouldShareCacheEntry()
    {
        var controller = Create<TeamController>();
        await controller.GetAllTeams(YEAR);

        await Create<TeamMcpTools>().GetAllTeams(YEAR);

        A.CallTo(() => _teamGetter.GetAllTeamsStats(YEAR)).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task GetTeamStats_CalledTwice_ShouldBuildOncePerTeam()
    {
        var cut = Create<TeamMcpTools>();

        await cut.GetTeamStats(1, YEAR);
        await cut.GetTeamStats(1, YEAR);
        await cut.GetTeamStats(2, YEAR);

        A.CallTo(() => _teamGetter.GetTeamStats(1, YEAR)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _teamGetter.GetTeamStats(2, YEAR)).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task GetGamesInDateRange_WithTimeOfDay_ShouldShareControllerCacheEntry()
    {
        var controller = Create<GameOddsController>();
        await controller.GetGameOddsInDateRange(new DateTime(2026, 10, 1), new DateTime(2026, 10, 7), YEAR);

        var result = await Create<GameOddsMcpTools>().GetGamesInDateRange(
            new DateTime(2026, 10, 1, 13, 30, 0), new DateTime(2026, 10, 7, 23, 0, 0), YEAR);

        result.Should().HaveCount(1);
        A.CallTo(() => _gameOddsGetter.GetGameOddsInDateRange(A<DateRange>._, YEAR)).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task GetTodaysGames_CalledTwice_ShouldBuildOnce()
    {
        var cut = Create<GameOddsMcpTools>();

        await cut.GetTodaysGames(YEAR);
        await cut.GetTodaysGames(YEAR);

        A.CallTo(() => _gameOddsGetter.GetGameOddsInDateRange(A<DateRange>._, YEAR)).MustHaveHappenedOnceExactly();
    }
}
