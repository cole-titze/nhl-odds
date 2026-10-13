using Entities.Models.Web;
using FluentAssertions;
using WebApi.BusinessLogic.GameOddsGetter;
using WebApi.Tests.BusinessLogic.Fakes;

namespace WebApi.Tests.BusinessLogic.UnitTests.GameOddsGetterTests;

[TestClass]
public class GameOddsGetterGetGameTests
{
    private static GameOdds GameWithId(int id) => new()
    {
        Game = new Game
        {
            Id = id,
            GameDate = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            HomeTeam = new Team { Id = 1 },
            AwayTeam = new Team { Id = 2 },
        },
    };

    [TestMethod]
    public async Task GetGameOdds_KnownId_ReturnsThatGame()
    {
        var getter = new GameOddsGetter(new FakeGameOddsRepository([GameWithId(2024020001), GameWithId(2024020002)]));

        var result = await getter.GetGameOdds(2024020002);

        result.Should().NotBeNull();
        result!.Id.Should().Be(2024020002);
    }

    [TestMethod]
    public async Task GetGameOdds_UnknownId_ReturnsNull()
    {
        var getter = new GameOddsGetter(new FakeGameOddsRepository([GameWithId(2024020001)]));

        var result = await getter.GetGameOdds(2024029999);

        result.Should().BeNull();
    }
}
