using Entities.ViewModels;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using WebApi.BusinessLogic.PlayerGetter;
using WebApi.Caching;
using WebApi.Controllers;
using WebApi.Mappers;
using WebApi.McpTools;

namespace WebApi.Tests.BusinessLogic.UnitTests.McpToolsTests;

[TestClass]
public class PlayerMcpToolsTests
{
    private const int YEAR = 2026;
    private const int MATTHEWS = 8479318;

    private IPlayerGetter _playerGetter = null!;
    private ServiceProvider _services = null!;

    private static PlayerVM Matthews() => new()
    {
        Id = MATTHEWS,
        FirstName = "Auston",
        LastName = "Matthews",
        Position = "C",
        SeasonStartYear = YEAR,
        CurrentTeam = new PlayerTeamVM { Id = 10, LocationName = "Toronto", TeamName = "Maple Leafs" },
        Games = new List<PlayerGameVM>
        {
            new()
            {
                GameId = 2026020020, GameDate = new DateTime(2026, 10, 21, 0, 0, 0, DateTimeKind.Utc), GameType = 2,
                IsHome = true, Opponent = new PlayerTeamVM { Id = 6, LocationName = "Boston", TeamName = "Bruins" },
                TeamGoals = 4, OpponentGoals = 1, Result = "W",
                Stats = new PlayerGameStatsVM { Goals = 2, Assists = 1, Points = 3, ShotsOnGoal = 6, PlusMinus = 2, TimeOnIceSeconds = 1215 },
            },
            new()
            {
                GameId = 2026020010, GameDate = new DateTime(2026, 10, 11, 0, 0, 0, DateTimeKind.Utc), GameType = 2,
                IsHome = false, Opponent = new PlayerTeamVM { Id = 6, LocationName = "Boston", TeamName = "Bruins" },
                TeamGoals = 2, OpponentGoals = 3, Result = "OTL",
                Stats = new PlayerGameStatsVM { Goals = 0, Assists = 1, Points = 1, ShotsOnGoal = 2, PlusMinus = -1, TimeOnIceSeconds = 1185 },
            },
        },
        SeasonTotals = new PlayerSeasonTotalsVM
        {
            GamesPlayed = 2, Goals = 2, Assists = 2, Points = 4, ShotsOnGoal = 8, PlusMinus = 1, TimeOnIceSeconds = 2400,
        },
    };

    [TestInitialize]
    public void Setup()
    {
        _playerGetter = A.Fake<IPlayerGetter>();
        A.CallTo(() => _playerGetter.GetPlayer(MATTHEWS, A<int>._)).ReturnsLazily(() => Matthews());
        A.CallTo(() => _playerGetter.GetPlayer(A<int>.That.Not.IsEqualTo(MATTHEWS), A<int>._)).Returns((PlayerVM?)null);
        A.CallTo(() => _playerGetter.FindPlayers(A<string>._, A<int>._, A<int>._))
            .ReturnsLazily(() => new List<PlayerSearchResultVM> { new() { Id = MATTHEWS, Name = "Auston Matthews" } });
        A.CallTo(() => _playerGetter.GetTeamRoster(A<int>._, A<int>._))
            .ReturnsLazily(() => new List<RosterPlayerVM> { new() { Id = MATTHEWS } });

        _services = new ServiceCollection()
            .AddSingleton(_playerGetter)
            .AddKeyedSingleton(ApiCache.ServiceKey, (_, _) => ApiCache.Create())
            .BuildServiceProvider();
    }

    [TestCleanup]
    public void Cleanup() => _services.Dispose();

    private T Create<T>() => ActivatorUtilities.CreateInstance<T>(_services);

    [TestMethod]
    public async Task FindPlayers_SameNameAnyCaseOrAccents_ShouldSearchOnce()
    {
        var cut = Create<PlayerMcpTools>();

        await cut.FindPlayers("Stützle");
        var result = await cut.FindPlayers("  STUTZLE ");

        result.Should().ContainSingle();
        A.CallTo(() => _playerGetter.FindPlayers("stutzle", A<int>._, 10)).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task FindPlayers_OneLetter_ShouldSayWhatsWrong()
    {
        var act = () => Create<PlayerMcpTools>().FindPlayers("m");

        await act.Should().ThrowAsync<McpException>().WithMessage("*2 and 60 letters*");
        A.CallTo(() => _playerGetter.FindPlayers(A<string>._, A<int>._, A<int>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task GetPlayer_AfterController_ShouldShareCacheEntry()
    {
        await Create<PlayerController>().GetPlayer(MATTHEWS, YEAR);

        var result = await Create<PlayerMcpTools>().GetPlayer(MATTHEWS, YEAR);

        result.Name.Should().Be("Auston Matthews");
        A.CallTo(() => _playerGetter.GetPlayer(MATTHEWS, YEAR)).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task GetPlayer_UnknownPlayer_ShouldPointToFindPlayers()
    {
        var act = () => Create<PlayerMcpTools>().GetPlayer(1, YEAR);

        await act.Should().ThrowAsync<McpException>().WithMessage("No player with ID 1*FindPlayers*");
    }

    [TestMethod]
    public void StatsMapper_OffEveryRoster_SaysWhoTheyLastPlayedFor()
    {
        var player = Matthews();
        player.CurrentTeam = null;
        player.LastTeam = new PlayerTeamVM { Id = 10, LocationName = "Toronto", TeamName = "Maple Leafs" };
        player.LastSeasonStartYear = 2011;

        var result = PlayerVmToPlayerStatsVmMapper.Map(player);

        result.CurrentTeam.Should().BeNull();
        result.LastPlayedFor.Should().Be("Toronto Maple Leafs (2011-12)");
    }

    [TestMethod]
    public void StatsMapper_StillOnLastTeam_NoLastPlayedFor_InjuredReserveKept()
    {
        var player = Matthews();
        player.LastTeam = player.CurrentTeam;
        player.LastSeasonStartYear = YEAR;
        player.IsInjuredReserve = true;

        var result = PlayerVmToPlayerStatsVmMapper.Map(player);

        result.LastPlayedFor.Should().BeNull();
        result.IsInjuredReserve.Should().BeTrue();
    }

    [TestMethod]
    public async Task GetPlayer_ShouldGiveTotalsAveragesAndCompactGames()
    {
        var result = await Create<PlayerMcpTools>().GetPlayer(MATTHEWS, YEAR);

        result.CurrentTeam.Should().Be("Toronto Maple Leafs");
        result.SeasonTotals.Points.Should().Be(4);
        result.SeasonTotals.TimeOnIce.Should().Be("40:00");
        result.PerGameAverages!.Points.Should().Be(2);
        result.PerGameAverages.ShotsOnGoal.Should().Be(4);
        result.PerGameAverages.TimeOnIce.Should().Be("20:00");
        result.PerGameAverages.Saves.Should().BeNull();
        var latest = result.Games[0];
        latest.Date.Should().Be("2026-10-20"); // 7 PM Central the evening before midnight UTC
        latest.Result.Should().Be("W 4-1");
        latest.Opponent.Should().Be("Boston Bruins");
        latest.TimeOnIce.Should().Be("20:15");
        latest.Saves.Should().BeNull();
        result.Games[1].Result.Should().Be("OTL 2-3");
        result.Games[1].Home.Should().BeFalse();
    }

    [TestMethod]
    public async Task GetTeamRoster_CalledTwice_ShouldBuildOncePerTeam()
    {
        var cut = Create<PlayerMcpTools>();

        await cut.GetTeamRoster(10, YEAR);
        await cut.GetTeamRoster(10, YEAR);
        await cut.GetTeamRoster(6, YEAR);

        A.CallTo(() => _playerGetter.GetTeamRoster(10, YEAR)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _playerGetter.GetTeamRoster(6, YEAR)).MustHaveHappenedOnceExactly();
    }
}
