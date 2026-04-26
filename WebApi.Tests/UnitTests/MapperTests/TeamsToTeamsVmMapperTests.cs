using Entities.Models.Web;
using Entities.Types;
using Entities.Types.Enums;
using FluentAssertions;
using WebApi.Mappers;

namespace WebApi.Tests.BusinessLogic.UnitTests.MapperTests;

[TestClass]
public class TeamsToTeamsVmMapperTests
{
    private static Team CreateTeam(int id) => new Team
    {
        Id = id,
        LocationName = $"City{id}",
        TeamName = $"Team{id}",
    };

    private static GameOdds CreatePlayedGameOdds(int homeTeamId, int awayTeamId, Winner winner, double logLoss = 0.5)
    {
        return new GameOdds
        {
            Game = new Game
            {
                Id = homeTeamId * 1000 + awayTeamId,
                HasBeenPlayed = true,
                Winner = winner,
                EndPeriod = PeriodType.Regulation,
                HomeTeam = CreateTeam(homeTeamId),
                AwayTeam = CreateTeam(awayTeamId),
                HomeGoals = winner == Winner.HOME ? 3 : 2,
                AwayGoals = winner == Winner.AWAY ? 3 : 2,
            },
            ModelHomeOdds = 0.6,
            ModelAwayOdds = 0.4,
            LogLoss = logLoss,
            ModelId = 1,
        };
    }

    [TestMethod]
    public void Map_WithNoTeams_ShouldReturnEmptyTeamsVM()
    {
        var result = TeamsToTeamsVmMapper.Map(new List<TeamStats>());

        result.Teams.Should().BeEmpty();
        result.SeasonTotals.TotalGameCount.Should().Be(0);
    }

    [TestMethod]
    public void Map_WithTwoTeams_ShouldReturnBothTeams()
    {
        var teams = new List<TeamStats>
        {
            new TeamStats { Team = CreateTeam(1), GameOdds = new List<GameOdds>() },
            new TeamStats { Team = CreateTeam(2), GameOdds = new List<GameOdds>() },
        };

        var result = TeamsToTeamsVmMapper.Map(teams);

        result.Teams.Should().HaveCount(2);
    }

    [TestMethod]
    public void Map_SeasonTotals_ShouldDivideByTwoForDoubleCountedGames()
    {
        // Two teams play each other — each team has the same game in their odds.
        // Season totals should divide by 2 since each game is counted once per team.
        var gameOdds = CreatePlayedGameOdds(1, 2, Winner.HOME, logLoss: 0.5);

        var teams = new List<TeamStats>
        {
            new TeamStats
            {
                Team = CreateTeam(1),
                GameOdds = new List<GameOdds> { gameOdds },
            },
            new TeamStats
            {
                Team = CreateTeam(2),
                GameOdds = new List<GameOdds> { gameOdds },
            },
        };

        var result = TeamsToTeamsVmMapper.Map(teams);

        // Each team has 1 played game, totals would be 2 before halving
        result.SeasonTotals.TotalGameCount.Should().Be(1);
    }

    [TestMethod]
    public void Map_SeasonTotals_ShouldCalculateWeightedAverageLogLoss()
    {
        var team1Game = CreatePlayedGameOdds(1, 3, Winner.HOME, logLoss: 0.4);
        var team2Game = CreatePlayedGameOdds(2, 4, Winner.HOME, logLoss: 0.6);

        var teams = new List<TeamStats>
        {
            new TeamStats
            {
                Team = CreateTeam(1),
                GameOdds = new List<GameOdds> { team1Game },
            },
            new TeamStats
            {
                Team = CreateTeam(2),
                GameOdds = new List<GameOdds> { team2Game },
            },
        };

        var result = TeamsToTeamsVmMapper.Map(teams);

        // Weighted average: (0.4 * 1 + 0.6 * 1) / 2 = 0.5
        result.SeasonTotals.ModelLogLoss.Should().BeApproximately(0.5, 0.001);
    }
}