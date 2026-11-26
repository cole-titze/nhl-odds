using Entities.Models.Web;
using FluentAssertions;
using WebApi.BusinessLogic.GameOddsGetter;
using WebApi.Tests.BusinessLogic.Fakes;

namespace WebApi.Tests.BusinessLogic.UnitTests.GameOddsGetterTests;

[TestClass]
public class GameOddsGetterBuildTests
{
    private const int SEASON = 2024;

    private static Team CreateTeam(int id) => new Team { Id = id, TeamName = $"Team{id}" };

    private static GameOdds CreateGameOdds(int homeTeamId, int awayTeamId)
    {
        return new GameOdds
        {
            Game = new Game
            {
                Id = homeTeamId * 100 + awayTeamId,
                HomeTeam = CreateTeam(homeTeamId),
                AwayTeam = CreateTeam(awayTeamId),
                HasBeenPlayed = true,
            },
            ModelHomeOdds = 0.5,
            ModelAwayOdds = 0.5,
        };
    }

    [TestMethod]
    public async Task BuildAllTeamsGameOdds_ShouldAssignGameOddsToCorrectTeams()
    {
        var allGameOdds = new List<GameOdds>
        {
            CreateGameOdds(1, 2),
            CreateGameOdds(1, 3),
            CreateGameOdds(2, 3),
        };
        var repo = new FakeGameOddsRepository(allGameOdds);
        var getter = new GameOddsGetter(repo);

        var teams = new List<TeamStats>
        {
            new TeamStats { Team = CreateTeam(1) },
            new TeamStats { Team = CreateTeam(2) },
            new TeamStats { Team = CreateTeam(3) },
        };

        var result = (await getter.BuildAllTeamsGameOdds(teams, SEASON)).ToList();

        // Team 1 is in games (1v2) and (1v3)
        result[0].GameOdds.Should().HaveCount(2);
        // Team 2 is in games (1v2) and (2v3)
        result[1].GameOdds.Should().HaveCount(2);
        // Team 3 is in games (1v3) and (2v3)
        result[2].GameOdds.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task BuildAllTeamsGameOdds_WithNoGameOdds_ShouldAssignEmptyLists()
    {
        var repo = new FakeGameOddsRepository(new List<GameOdds>());
        var getter = new GameOddsGetter(repo);

        var teams = new List<TeamStats>
        {
            new TeamStats { Team = CreateTeam(1) },
        };

        var result = (await getter.BuildAllTeamsGameOdds(teams, SEASON)).ToList();

        result[0].GameOdds.Should().BeEmpty();
    }

    [TestMethod]
    public async Task BuildTeamGameOdds_ShouldPopulateTeamGameOdds()
    {
        var allGameOdds = new List<GameOdds>
        {
            CreateGameOdds(1, 2),
            CreateGameOdds(3, 4),
        };
        var repo = new FakeGameOddsRepository(allGameOdds);
        var getter = new GameOddsGetter(repo);

        var team = new TeamStats { Team = CreateTeam(1) };

        var result = await getter.BuildTeamGameOdds(team, SEASON);

        // FakeGameOddsRepository.GetTeamGameOdds returns all items (not filtered)
        // In real usage the repo filters by team, but the BL just assigns the result
        result.GameOdds.Should().NotBeNull();
    }
}
