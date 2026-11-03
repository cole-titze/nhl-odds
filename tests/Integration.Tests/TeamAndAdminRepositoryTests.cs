using DatabaseAccess.WebAdminRepository;
using DatabaseAccess.WebTeamRepository;

namespace Integration.Tests;

/// <summary>DatabaseAccess.WebTeamRepository and the Admin page's health checks (WebAdminRepository).</summary>
[TestClass]
[TestCategory(TestDatabase.Category)]
public class TeamAndAdminRepositoryTests
{
    private static readonly DateTime Start = new(2026, 11, 5, 0, 0, 0);

    [TestInitialize]
    public async Task Setup()
    {
        await TestDatabase.Reset();
        await Seed.DefaultTeams();
    }

    [TestMethod]
    public async Task GetAllTeams_ShouldListTheSeasonsTeams()
    {
        await Seed.Teams(new Seed.Team(20, "CGY", "Calgary", "Flames", Season: 2025));
        await using var db = TestContexts.Web();

        var teams = await new TeamRepository(db).GetAllTeams(Seed.Season);

        teams.Select(t => t.Team.Id).Should().BeEquivalentTo([Seed.Leafs, Seed.Bruins]);
    }

    [TestMethod]
    public async Task GetTeam_ShouldMapTheSeasonTeam()
    {
        await using var db = TestContexts.Web();

        var team = await new TeamRepository(db).GetTeam(Seed.Leafs, Seed.Season);

        team.Team.TeamName.Should().Be("Maple Leafs");
        team.Team.LocationName.Should().Be("Toronto");
    }

    [TestMethod]
    public async Task GetTeam_UnknownTeam_Throws()
    {
        // Documents today's behaviour: the API answers an unknown team with a 500, not a 404
        await using var db = TestContexts.Web();

        var act = () => new TeamRepository(db).GetTeam(99, Seed.Season);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [TestMethod]
    public async Task GetHealthChecks_ShouldCountWhatEachSeasonIsMissing()
    {
        await Seed.Games(
            new Seed.Game(1, Start.AddDays(-10)) { Played = true },
            new Seed.Game(2, Start.AddDays(-9)) { Played = true },
            new Seed.Game(3, Start.AddDays(30)),
            new Seed.Game(4, Start.AddDays(-8)) { Played = true, GameType = 3 });  // playoffs aren't counted
        await Seed.Predictions(new Seed.GameOdds(1, Start.AddDays(-11), 0.5));
        await Seed.Bookmakers(
            new Seed.BookmakerOdds(1, "DraftKings", 0.5) { MarketLastUpdateUtc = Start.AddDays(-11) },
            // Updated after puck drop: an in-game price
            new Seed.BookmakerOdds(2, "DraftKings", 0.5) { MarketLastUpdateUtc = Start.AddDays(-9).AddHours(1) });
        await using var db = TestContexts.Web();

        var checks = await new WebAdminRepository(db).GetHealthChecks();

        var season = checks.Single(c => c.SeasonStartYear == Seed.Season);
        season.TotalGames.Should().Be(3);
        season.PlayedGames.Should().Be(2);
        // Moneyline, spread and total predictions: game 1 has only the moneyline
        season.MissingPredictions.Should().Be(2 + 3 + 3);
        season.LiveBookmakerOdds.Should().Be(1);
        season.MissingGameCleaned.Should().Be(3);
    }
}
