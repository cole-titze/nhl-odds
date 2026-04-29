using Entities.Models.Web;
using FluentAssertions;
using WebApi.BusinessLogic.GameOddsGetter;
using WebApi.Tests.BusinessLogic.Fakes;

namespace WebApi.Tests.BusinessLogic.UnitTests.GameOddsGetterTests;

[TestClass]
public class GameOddsGetterAnchorDateTests
{
    private const int SEASON = 2024;

    // 2025-01-15 12:00 UTC = 06:00 CT, well into a normal Central day.
    private static readonly DateTime NowUtc = new(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    private static GameOdds GameAt(DateTime gameDateUtc) => new()
    {
        Game = new Game
        {
            Id = (int)(gameDateUtc.Ticks % int.MaxValue),
            GameDate = gameDateUtc,
            HomeTeam = new Team { Id = 1 },
            AwayTeam = new Team { Id = 2 },
        },
    };

    [TestMethod]
    public async Task GetAnchorDate_OnlyUpcomingGames_ReturnsEarliestUpcomingDate()
    {
        var games = new List<GameOdds>
        {
            GameAt(new DateTime(2025, 1, 20, 0, 0, 0, DateTimeKind.Utc)),
            GameAt(new DateTime(2025, 1, 18, 0, 0, 0, DateTimeKind.Utc)),
            GameAt(new DateTime(2025, 1, 25, 0, 0, 0, DateTimeKind.Utc)),
        };
        var getter = new GameOddsGetter(new FakeGameOddsRepository(games, NowUtc));

        var result = await getter.GetAnchorDate(SEASON);

        // 2025-01-18 00:00 UTC -6h = 2025-01-17 18:00 CT → date bucket 2025-01-17
        result.AnchorDate.Should().Be(new DateTime(2025, 1, 17));
    }

    [TestMethod]
    public async Task GetAnchorDate_OnlyPastGames_ReturnsMostRecentPastDate()
    {
        var games = new List<GameOdds>
        {
            GameAt(new DateTime(2025, 1, 5, 18, 0, 0, DateTimeKind.Utc)),
            GameAt(new DateTime(2025, 1, 10, 18, 0, 0, DateTimeKind.Utc)),
            GameAt(new DateTime(2024, 12, 28, 18, 0, 0, DateTimeKind.Utc)),
        };
        var getter = new GameOddsGetter(new FakeGameOddsRepository(games, NowUtc));

        var result = await getter.GetAnchorDate(SEASON);

        // 2025-01-10 18:00 UTC -6h = 2025-01-10 12:00 CT → date bucket 2025-01-10
        result.AnchorDate.Should().Be(new DateTime(2025, 1, 10));
    }

    [TestMethod]
    public async Task GetAnchorDate_MixedGames_PrefersUpcomingOverPast()
    {
        var games = new List<GameOdds>
        {
            GameAt(new DateTime(2025, 1, 10, 18, 0, 0, DateTimeKind.Utc)), // past
            GameAt(new DateTime(2025, 1, 20, 18, 0, 0, DateTimeKind.Utc)), // upcoming
            GameAt(new DateTime(2025, 1, 25, 18, 0, 0, DateTimeKind.Utc)), // upcoming
        };
        var getter = new GameOddsGetter(new FakeGameOddsRepository(games, NowUtc));

        var result = await getter.GetAnchorDate(SEASON);

        // 2025-01-20 18:00 UTC -6h = 2025-01-20 12:00 CT → 2025-01-20
        result.AnchorDate.Should().Be(new DateTime(2025, 1, 20));
    }

    [TestMethod]
    public async Task GetAnchorDate_EmptySeason_ReturnsNull()
    {
        var getter = new GameOddsGetter(new FakeGameOddsRepository(new List<GameOdds>(), NowUtc));

        var result = await getter.GetAnchorDate(SEASON);

        result.AnchorDate.Should().BeNull();
    }

    [TestMethod]
    public async Task GetAnchorDate_GameAtUtcMidnightEarlyMorning_BelongsToPreviousCentralDay()
    {
        // 2025-01-16 02:00 UTC = 2025-01-15 20:00 CT → date bucket 2025-01-15.
        // Relative to NowUtc (2025-01-15 12:00 UTC = 06:00 CT, central date = 2025-01-15),
        // this game is "today" in Central, so it counts as upcoming (>= today).
        var games = new List<GameOdds>
        {
            GameAt(new DateTime(2025, 1, 16, 2, 0, 0, DateTimeKind.Utc)),
        };
        var getter = new GameOddsGetter(new FakeGameOddsRepository(games, NowUtc));

        var result = await getter.GetAnchorDate(SEASON);

        result.AnchorDate.Should().Be(new DateTime(2025, 1, 15));
    }
}