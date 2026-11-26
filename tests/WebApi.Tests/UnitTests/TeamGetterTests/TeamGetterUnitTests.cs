using Entities.Models.Web;
using Entities.ViewModels;
using FluentAssertions;
using WebApi.BusinessLogic.GameOddsGetter;
using WebApi.BusinessLogic.TeamGetter;
using WebApi.Tests.BusinessLogic.Fakes;

namespace WebApi.Tests.BusinessLogic.UnitTests.TeamGetterTests;

[TestClass]
public class TeamGetterUnitTests
{
    private const int YEAR = 2021;

    public List<TeamStats> TeamsFactory(int numberOfTeams)
    {
        var teamList = new List<TeamStats>();
        for (int i = 0; i < numberOfTeams; i++)
        {
            var team = new TeamStats()
            {
                Team = new Team()
                {
                    Id = i,
                }
            };
            teamList.Add(team);
        }
        return teamList;
    }

    public TeamGetter Factory(int numberOfTeams)
    {
        var teamList = TeamsFactory(numberOfTeams);
        var teamRepo = new FakeTeamRepository(teamList);
        var gameOddsRepo = new FakeGameOddsRepository(new List<GameOdds>());
        var gameOddsGetter = new GameOddsGetter(gameOddsRepo);

        var cut = new TeamGetter(teamRepo, gameOddsGetter);
        return cut;
    }

    [TestMethod]
    public async Task CallToGetAllTeamsStats_WithZeroTeams_ShouldGetZeroTeams()
    {
        int numberOfTeams = 0;
        var cut = Factory(numberOfTeams);

        var result = await cut.GetAllTeamsStats(YEAR);
        result.Teams.Should().HaveCount(0);
    }

    [TestMethod]
    public async Task CallToGetAllTeamsStats_WithFiveTeams_ShouldGetFiveTeams()
    {
        int numberOfTeams = 5;
        var cut = Factory(numberOfTeams);

        var result = await cut.GetAllTeamsStats(YEAR);
        result.Teams.Should().HaveCount(5);
    }
}
