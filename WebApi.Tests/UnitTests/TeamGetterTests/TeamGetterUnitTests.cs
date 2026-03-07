namespace WebApi.Tests.BusinessLogic.UnitTests.TeamGetterTests;
using FluentAssertions;
using WebApi.BusinessLogic.TeamGetter;
using Entities.Models.Web;
using WebApi.Tests.BusinessLogic.Fakes;

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
                team = new Team()
                {
                    id = i,
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

        var cut = new TeamGetter(teamRepo);

        return cut;
    }
    [TestMethod]
    public async Task CallToGetAllTeamsStats_WithZeroTeams_ShouldGetZeroTeams()
    {
        int numberOfTeams = 0;
        var cut = Factory(numberOfTeams);

        var teams = await cut.GetAllTeamsStats(YEAR);
        teams.Should().HaveCount(0);
    }
    [TestMethod]
    public async Task CallToGetAllTeamsStats_WithFiveTeams_ShouldGetFiveTeams()
    {
        int numberOfTeams = 5;
        var cut = Factory(numberOfTeams);

        var teams = await cut.GetAllTeamsStats(YEAR);
        teams.Should().HaveCount(5);
    }
}
