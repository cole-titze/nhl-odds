using Entities.Models.Web;
using Entities.Types;
using FakeItEasy;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using ModelContextProtocol;
using WebApi.BusinessLogic.PlayerGetter;
using WebApi.BusinessLogic.StatLeaders;
using WebApi.Caching;
using WebApi.Controllers;
using WebApi.McpTools;
using WebApi.Tests.BusinessLogic.Fakes;

namespace WebApi.Tests.BusinessLogic.UnitTests.StatLeadersTests;

[TestClass]
public class StatLeadersTests
{
    private const int YEAR = 2025;

    private static PlayerSeasonTotals Skater(int id, string last, int gp, int goals, int assists, int toi = 0) => new()
    {
        PlayerId = id, FirstName = "P", LastName = last, Position = POSITION.Center, TeamId = 10,
        TeamAbbreviation = "TOR", GamesPlayed = gp, Goals = goals, Assists = assists, TimeOnIceSeconds = toi,
    };

    private static PlayerSeasonTotals Goalie(int id, string last, int gp, int saves, int ga, int toi) => new()
    {
        PlayerId = id, FirstName = "G", LastName = last, Position = POSITION.Goalie, IsGoalie = true, TeamId = 6,
        GamesPlayed = gp, Saves = saves, GoalsAgainst = ga, TimeOnIceSeconds = toi,
    };

    private static StatLeadersVMResult Rank(string stat, params PlayerSeasonTotals[] totals) =>
        new(StatLeaders.Rank(totals, StatLeaders.Find(stat)!, YEAR));

    private sealed record StatLeadersVMResult(Entities.ViewModels.StatLeadersVM Vm);

    [TestMethod]
    public void Points_RanksBySumWithSharedRanksOnTies()
    {
        var vm = Rank("points",
            Skater(1, "A", 10, 5, 5),   // 10
            Skater(2, "B", 10, 8, 4),   // 12
            Skater(3, "C", 9, 3, 7),    // 10, fewer games
            Skater(4, "D", 10, 1, 1)).Vm;

        vm.Leaders.Select(l => (l.PlayerId, l.Rank, l.Value)).Should().Equal((2, 1, 12.0), (3, 2, 10.0), (1, 2, 10.0), (4, 4, 2.0));
        vm.Group.Should().Be("skater");
        vm.MinGamesPlayed.Should().Be(0);
        vm.Leaders[0].Position.Should().Be("C");
        vm.Leaders[0].TeamAbbreviation.Should().Be("TOR");
    }

    [TestMethod]
    public void SkaterStats_LeaveOutGoaliesAndPlayersWithoutGames()
    {
        var vm = Rank("goals", Skater(1, "A", 0, 0, 0), Skater(2, "B", 3, 1, 0), Goalie(3, "G", 3, 90, 5, 10800)).Vm;

        vm.Leaders.Select(l => l.PlayerId).Should().Equal(2);
    }

    [TestMethod]
    public void SavePercentage_NeedsAQuarterOfTheMostGames()
    {
        var vm = Rank("savePercentage",
            Goalie(1, "Starter", 20, 540, 40, 72000),  // .931
            Goalie(2, "OneGame", 1, 30, 0, 3600),      // 1.000 but 1 game < 5
            Goalie(3, "Backup", 5, 130, 12, 18000)).Vm; // .915

        vm.MinGamesPlayed.Should().Be(5);
        vm.Leaders.Select(l => l.PlayerId).Should().Equal(1, 3);
        vm.Leaders[0].Value.Should().BeApproximately(540.0 / 580, 1e-9);
    }

    [TestMethod]
    public void SavePercentage_TiesAsShown()
    {
        var vm = Rank("savePercentage",
            Goalie(1, "A", 10, 9254, 746, 36000),  // .9254 -> .925
            Goalie(2, "B", 10, 9246, 754, 36000),  // .9246 -> .925
            Goalie(3, "C", 10, 9100, 900, 36000)).Vm; // .910

        vm.Leaders.Select(l => (l.PlayerId, l.Rank)).Should().Equal((1, 1), (2, 1), (3, 3));
    }

    [TestMethod]
    public void GoalsAgainstAverage_LowestLeads()
    {
        var vm = Rank("goalsAgainstAverage",
            Goalie(1, "A", 10, 250, 30, 36000),  // 3.00
            Goalie(2, "B", 10, 260, 20, 36000)).Vm; // 2.00

        vm.LowerIsBetter.Should().BeTrue();
        vm.Leaders.Select(l => (l.PlayerId, l.Value)).Should().Equal((2, 2.0), (1, 3.0));
    }

    [TestMethod]
    public void TimeOnIcePerGame_IsSecondsPerGame()
    {
        var vm = Rank("timeOnIcePerGame", Skater(1, "A", 4, 0, 0, 4 * 1200), Skater(2, "B", 4, 0, 0, 4 * 1500)).Vm;

        vm.Leaders.Select(l => (l.PlayerId, l.Value)).Should().Equal((2, 1500.0), (1, 1200.0));
    }

    [TestMethod]
    public void Rank_StopsAtTheLimit()
    {
        var totals = Enumerable.Range(1, 80).Select(i => Skater(i, $"P{i}", 10, i, 0)).ToArray();

        StatLeaders.Rank(totals, StatLeaders.Find("goals")!, YEAR).Leaders.Should().HaveCount(StatLeaders.MaxLeaders);
        StatLeaders.Rank(totals, StatLeaders.Find("goals")!, YEAR, limit: 10).Leaders.Should().HaveCount(10);
    }

    [TestMethod]
    public void Find_IgnoresCaseAndRejectsUnknownStats()
    {
        StatLeaders.Find("SavePercentage")!.Key.Should().Be("savePercentage");
        StatLeaders.Find("wins").Should().BeNull();
        StatLeaders.Find(null).Should().BeNull();
    }

    [TestMethod]
    public async Task Getter_ReadsTheSeasonOnceForEveryStat()
    {
        var repo = new FakeWebPlayerRepository();
        repo.SeasonTotals.Add(Skater(1, "A", 10, 5, 5));
        var getter = new StatLeadersGetter(repo, ApiCache.Create());

        (await getter.GetStatLeaders("points", YEAR))!.Leaders.Should().HaveCount(1);
        (await getter.GetStatLeaders("goals", YEAR))!.Leaders.Should().HaveCount(1);
        (await getter.GetStatLeaders("nope", YEAR)).Should().BeNull();

        repo.SeasonTotalsCalls.Should().Be(1);
    }

    [TestMethod]
    public async Task Controller_RejectsUnknownStatsAndSeasons()
    {
        var repo = new FakeWebPlayerRepository();
        var controller = new LeadersController(new StatLeadersGetter(repo, ApiCache.Create()));

        (await controller.GetStatLeaders("points", YEAR)).Should().BeOfType<Ok<Entities.ViewModels.StatLeadersVM>>();
        (await controller.GetStatLeaders("wins", YEAR)).Should().BeOfType<BadRequest<string>>();
        (await controller.GetStatLeaders("points", 1990)).Should().BeOfType<BadRequest<string>>();
        (await controller.GetStatLeaders("points", NhlSeason.Current(DateTime.UtcNow) + 1)).Should().BeOfType<BadRequest<string>>();
    }

    [TestMethod]
    public async Task McpTool_ReturnsTheLimitAndExplainsUnknownStats()
    {
        var repo = new FakeWebPlayerRepository();
        repo.SeasonTotals.AddRange(Enumerable.Range(1, 30).Select(i => Skater(i, $"P{i}", 10, i, 0)));
        var tools = new PlayerMcpTools(A.Fake<IPlayerGetter>(), ApiCache.Create(),
            new StatLeadersGetter(repo, ApiCache.Create()));

        (await tools.GetStatLeaders("goals", YEAR)).Leaders.Should().HaveCount(10);
        (await tools.GetStatLeaders("goals", YEAR, 200)).Leaders.Should().HaveCount(30);

        var unknown = () => tools.GetStatLeaders("wins", YEAR);
        await unknown.Should().ThrowAsync<McpException>().WithMessage("*savePercentage*");
        var tooOld = () => tools.GetStatLeaders("goals", 1990);
        await tooOld.Should().ThrowAsync<McpException>();
    }
}
