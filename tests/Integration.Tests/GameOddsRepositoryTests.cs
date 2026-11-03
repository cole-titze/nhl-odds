using Entities.Types;

namespace Integration.Tests;

/// <summary>The Games, Game and Team pages' queries (DatabaseAccess.WebGameOddsRepository).</summary>
[TestClass]
[TestCategory(TestDatabase.Category)]
public class GameOddsRepositoryTests
{
    private static readonly TimeZoneInfo Central = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");

    [TestInitialize]
    public async Task Setup()
    {
        await TestDatabase.Reset();
        await Seed.DefaultTeams();
    }

    // The times below fall on the same Central date in both CST and CDT: the date filter
    // uses today's UTC offset, so the tests mustn't depend on when they run
    private static readonly DateTime Nov4Noon = new(2026, 11, 4, 12, 0, 0);    // Nov 4 CT
    private static readonly DateTime Nov4Evening = new(2026, 11, 5, 2, 0, 0);  // Nov 4 CT, Nov 5 UTC
    private static readonly DateTime Nov3Evening = new(2026, 11, 4, 3, 0, 0);  // Nov 3 CT, Nov 4 UTC

    [TestMethod]
    public async Task GetGameOddsInDateRange_ShouldFilterOnCentralDates()
    {
        await Seed.Games(new Seed.Game(1, Nov4Evening), new Seed.Game(2, Nov4Noon), new Seed.Game(3, Nov3Evening));
        await using var db = TestContexts.Web();

        var games = await TestContexts.GameOdds(db).GetGameOddsInDateRange(
            new DateRange { StartDate = new DateTime(2026, 11, 4), EndDate = new DateTime(2026, 11, 4) }, Seed.Season);

        games.Select(g => g.Game.Id).Should().Equal(2, 1);
    }

    [TestMethod]
    public async Task GetGameOdds_ShouldReturnTheGameInUtcWithItsTeams()
    {
        await Seed.Games(new Seed.Game(1, Nov4Evening));
        await using var db = TestContexts.Web();

        var game = await TestContexts.GameOdds(db).GetGameOdds(1);

        game.Should().NotBeNull();
        game!.Game.GameDate.Should().Be(Nov4Evening);
        game.Game.GameDate.Kind.Should().Be(DateTimeKind.Utc);
        game.Game.HomeTeam.TeamName.Should().Be("Maple Leafs");
        game.Game.AwayTeam.LocationName.Should().Be("Boston");
        game.ModelHomeOdds.Should().BeNull("no prediction has been saved");
    }

    [TestMethod]
    public async Task GetGameOdds_UnknownGame_ShouldBeNull()
    {
        await using var db = TestContexts.Web();

        (await TestContexts.GameOdds(db).GetGameOdds(404)).Should().BeNull();
    }

    [TestMethod]
    public async Task GetGameOdds_ShouldUseTheNewestPredictionRun()
    {
        await Seed.Games(new Seed.Game(1, Nov4Evening));
        await Seed.Predictions(
            new Seed.GameOdds(1, new DateTime(2026, 11, 3, 12, 0, 0), 0.55),
            new Seed.GameOdds(1, new DateTime(2026, 11, 4, 12, 0, 0), 0.61),
            new Seed.GameOdds(1, new DateTime(2026, 11, 2, 12, 0, 0), 0.40));
        await using var db = TestContexts.Web();

        var game = await TestContexts.GameOdds(db).GetGameOdds(1);

        game!.ModelHomeOdds.Should().BeApproximately(0.61, 1e-9);
        game.ModelAwayOdds.Should().BeApproximately(0.39, 1e-9);
        game.ModelId.Should().Be(1);
    }

    [TestMethod]
    public async Task GetGameOdds_PlayedGame_ShouldComputeLogLossFromThePrediction()
    {
        await Seed.Games(new Seed.Game(1, Nov4Evening) { Played = true, HomeGoals = 3, AwayGoals = 1, Winner = 0 });
        await Seed.Predictions(new Seed.GameOdds(1, new DateTime(2026, 11, 4, 12, 0, 0), 0.6));
        await using var db = TestContexts.Web();

        var game = await TestContexts.GameOdds(db).GetGameOdds(1);

        game!.LogLoss.Should().BeApproximately(-Math.Log(0.6), 1e-9);
        game.Game.HomeGoals.Should().Be(3);
    }

    [TestMethod]
    public async Task GetGameOdds_ShouldAttachEachBooksOdds()
    {
        await Seed.Games(new Seed.Game(1, Nov4Evening), new Seed.Game(2, Nov4Noon));
        await Seed.Bookmakers(
            new Seed.BookmakerOdds(1, "DraftKings", 0.52),
            new Seed.BookmakerOdds(1, "Kalshi", 0.50),
            new Seed.BookmakerOdds(2, "DraftKings", 0.70));
        await using var db = TestContexts.Web();

        var game = await TestContexts.GameOdds(db).GetGameOdds(1);

        game!.BookmakerOdds.Select(b => b.BookmakerName).Should().BeEquivalentTo("DraftKings", "Kalshi");
        game.BookmakerOdds.Single(b => b.BookmakerName == "DraftKings").HomeOdds.Should().BeApproximately(0.52, 1e-9);
    }

    [TestMethod]
    public async Task GetTeamGameOdds_ShouldListTheSeasonsGamesNewestFirst()
    {
        await Seed.Teams(new Seed.Team(20, "CGY", "Calgary", "Flames"), Seed.TorontoMapleLeafs with { Season = 2025 });
        await Seed.Games(
            new Seed.Game(1, Nov3Evening),
            new Seed.Game(2, Nov4Evening) { HomeTeamId = Seed.Bruins, AwayTeamId = Seed.Leafs },
            new Seed.Game(3, Nov4Noon) { HomeTeamId = 20, AwayTeamId = Seed.Bruins },
            new Seed.Game(4, new DateTime(2026, 3, 1, 0, 0, 0)) { Season = 2025 });
        await using var db = TestContexts.Web();

        var games = await TestContexts.GameOdds(db).GetTeamGameOdds(Seed.Leafs, Seed.Season);

        games.Select(g => g.Game.Id).Should().Equal(2, 1);
    }

    [TestMethod]
    public async Task GetAnchorDate_ShouldBeTheNextGameDayInCentralTime()
    {
        var now = DateTime.UtcNow;
        // Evening games, so the Central date is the day before the UTC one
        var next = now.Date.AddDays(3).AddHours(2);
        await Seed.Games(new Seed.Game(1, now.Date.AddDays(-2).AddHours(2)), new Seed.Game(2, next),
            new Seed.Game(3, next.AddDays(1)));
        await using var db = TestContexts.Web();

        var anchor = await TestContexts.GameOdds(db).GetAnchorDate(Seed.Season);

        anchor.Should().Be(TimeZoneInfo.ConvertTimeFromUtc(next, Central).Date);
    }

    [TestMethod]
    public async Task GetAnchorDate_SeasonOver_ShouldBeTheLastGameDay()
    {
        var last = DateTime.UtcNow.Date.AddDays(-5).AddHours(2);
        await Seed.Games(new Seed.Game(1, last.AddDays(-1)), new Seed.Game(2, last));
        await using var db = TestContexts.Web();

        var anchor = await TestContexts.GameOdds(db).GetAnchorDate(Seed.Season);

        anchor.Should().Be(TimeZoneInfo.ConvertTimeFromUtc(last, Central).Date);
    }

    [TestMethod]
    public async Task GetAnchorDate_NoGames_ShouldBeNull()
    {
        await using var db = TestContexts.Web();

        (await TestContexts.GameOdds(db).GetAnchorDate(Seed.Season)).Should().BeNull();
    }
}
