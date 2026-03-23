using Entities.Models.Web;
using Entities.Types;
using FluentAssertions;
using WebApi.BusinessLogic.GameOddsGetter;
using WebApi.Tests.BusinessLogic.Fakes;

namespace WebApi.Tests.BusinessLogic.UnitTests.TeamGetterTests;

[TestClass]
public class GameOddsGetterUnitTests
{
    private const int SEASON = 2021;
    private readonly DateRange dateRange = new DateRange()
    {
        StartDate = DateTime.Parse("1/1/2000"),
        EndDate = DateTime.Parse("1/1/2010")
    };
    private readonly DateTime dateInRange = DateTime.Parse("1/1/2005");
    private readonly DateTime dateOutOfRange = DateTime.Parse("1/1/2020");

    public List<GameOdds> GamesFactory(int numberOfGamesInDateRange, int numberOfGamesOutOfDateRange)
    {
        var gameList = new List<GameOdds>();
        for (int i = 0; i < numberOfGamesInDateRange; i++)
        {
            var game = new GameOdds()
            {
                Game = new Game()
                {
                    GameDate = dateInRange,
                    HomeTeam = new Team { Id = 1 },
                    AwayTeam = new Team { Id = 2 },
                }
            };
            gameList.Add(game);
        }
        for (int i = 0; i < numberOfGamesOutOfDateRange; i++)
        {
            var game = new GameOdds()
            {
                Game = new Game()
                {
                    GameDate = dateOutOfRange,
                    HomeTeam = new Team { Id = 1 },
                    AwayTeam = new Team { Id = 2 },
                }
            };
            gameList.Add(game);
        }
        return gameList;
    }

    public GameOddsGetter Factory(int numberOfGamesInDateRange, int numberOfGamesOutOfDateRange)
    {
        var gameList = GamesFactory(numberOfGamesInDateRange, numberOfGamesOutOfDateRange);
        var gameRepo = new FakeGameOddsRepository(gameList);

        var cut = new GameOddsGetter(gameRepo);

        return cut;
    }

    [TestMethod]
    public async Task CallToGetGameOddsInDateRange_WithNoGamesInDateRange_ShouldGetNoGames()
    {
        int numberOfGamesInDateRange = 0;
        int numberOfGamesOutOfDateRange = 5;
        var cut = Factory(numberOfGamesInDateRange, numberOfGamesOutOfDateRange);

        var games = await cut.GetGameOddsInDateRange(dateRange, SEASON);
        games.Should().HaveCount(0);
    }

    [TestMethod]
    public async Task CallToGetGameOddsInDateRange_WithFiveGamesInDateRange_ShouldGetFiveGames()
    {
        int numberOfGamesInDateRange = 5;
        int numberOfGamesOutOfDateRange = 0;
        var cut = Factory(numberOfGamesInDateRange, numberOfGamesOutOfDateRange);

        var games = await cut.GetGameOddsInDateRange(dateRange, SEASON);
        games.Should().HaveCount(5);
    }

    [TestMethod]
    public async Task CallToGetGameOddsInDateRange_WithFiveGamesInDateRangeAndFiftyGamesOutOfDateRange_ShouldGetFiveGames()
    {
        int numberOfGamesInDateRange = 5;
        int numberOfGamesOutOfDateRange = 50;
        var cut = Factory(numberOfGamesInDateRange, numberOfGamesOutOfDateRange);

        var games = await cut.GetGameOddsInDateRange(dateRange, SEASON);
        games.Should().HaveCount(5);
    }

    [TestMethod]
    public async Task CallToGetGameOddsInDateRange_WitZeroGamesInDateRangeAndZeroGamesOutOfDateRange_ShouldGetZeroGames()
    {
        int numberOfGamesInDateRange = 0;
        int numberOfGamesOutOfDateRange = 0;
        var cut = Factory(numberOfGamesInDateRange, numberOfGamesOutOfDateRange);

        var games = await cut.GetGameOddsInDateRange(dateRange, SEASON);
        games.Should().HaveCount(0);
    }
}
