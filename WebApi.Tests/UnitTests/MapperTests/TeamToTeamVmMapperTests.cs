using Entities.Models.Web;
using Entities.Types;
using Entities.Types.Enums;
using FluentAssertions;
using WebApi.Mappers;

namespace WebApi.Tests.BusinessLogic.UnitTests.MapperTests;

[TestClass]
public class TeamToTeamVmMapperTests
{
    private static Team CreateTeam(int id = 1) => new Team
    {
        Id = id,
        LocationName = "Test City",
        TeamName = "Testers",
        LogoUri = "https://example.com/logo.png",
    };

    private static Game CreatePlayedGame(int homeTeamId, int awayTeamId, Winner winner, PeriodType endPeriod = PeriodType.Regulation)
    {
        return new Game
        {
            Id = 2024020001,
            GameDate = new DateTime(2024, 1, 15),
            HomeGoals = winner == Winner.HOME ? 3 : 2,
            AwayGoals = winner == Winner.AWAY ? 3 : 2,
            SeasonStartYear = 2024,
            Winner = winner,
            EndPeriod = endPeriod,
            HasBeenPlayed = true,
            HomeTeam = CreateTeam(homeTeamId),
            AwayTeam = CreateTeam(awayTeamId),
        };
    }

    private static GameOdds CreateGameOdds(Game game, double homeOdds = 0.6, double awayOdds = 0.4, double logLoss = 0.5)
    {
        return new GameOdds
        {
            Game = game,
            ModelHomeOdds = homeOdds,
            ModelAwayOdds = awayOdds,
            LogLoss = logLoss,
            ModelId = 1,
        };
    }

    [TestMethod]
    public void Map_WithNoGames_ShouldReturnTeamWithZeroStats()
    {
        var teamStats = new TeamStats
        {
            Team = CreateTeam(1),
            GameOdds = new List<GameOdds>(),
        };

        var result = TeamToTeamVmMapper.Map(teamStats);

        result.Id.Should().Be(1);
        result.LocationName.Should().Be("Test City");
        result.TeamName.Should().Be("Testers");
        result.LogoUri.Should().Be("https://example.com/logo.png");
        result.TotalGameCount.Should().Be(0);
        result.SeasonWins.Should().Be(0);
        result.SeasonLosses.Should().Be(0);
        result.SeasonOvertimeLosses.Should().Be(0);
        result.ModelLogLoss.Should().Be(0);
        result.TotalModelAccurateGameCount.Should().Be(0);
    }

    [TestMethod]
    public void Map_WithHomeWins_ShouldCountWinsCorrectly()
    {
        var teamStats = new TeamStats
        {
            Team = CreateTeam(1),
            GameOdds = new List<GameOdds>
            {
                CreateGameOdds(CreatePlayedGame(1, 2, Winner.HOME)),
                CreateGameOdds(CreatePlayedGame(1, 3, Winner.HOME)),
            },
        };

        var result = TeamToTeamVmMapper.Map(teamStats);

        result.SeasonWins.Should().Be(2);
        result.SeasonLosses.Should().Be(0);
        result.TotalGameCount.Should().Be(2);
    }

    [TestMethod]
    public void Map_WithAwayWins_ShouldCountWinsCorrectly()
    {
        var teamStats = new TeamStats
        {
            Team = CreateTeam(1),
            GameOdds = new List<GameOdds>
            {
                CreateGameOdds(CreatePlayedGame(2, 1, Winner.AWAY)),
            },
        };

        var result = TeamToTeamVmMapper.Map(teamStats);

        result.SeasonWins.Should().Be(1);
        result.SeasonLosses.Should().Be(0);
    }

    [TestMethod]
    public void Map_WithRegulationLoss_ShouldCountAsRegulationLoss()
    {
        var teamStats = new TeamStats
        {
            Team = CreateTeam(1),
            GameOdds = new List<GameOdds>
            {
                CreateGameOdds(CreatePlayedGame(1, 2, Winner.AWAY, PeriodType.Regulation)),
            },
        };

        var result = TeamToTeamVmMapper.Map(teamStats);

        result.SeasonWins.Should().Be(0);
        result.SeasonLosses.Should().Be(1);
        result.SeasonOvertimeLosses.Should().Be(0);
    }

    [TestMethod]
    public void Map_WithOvertimeLoss_ShouldCountAsOvertimeLoss()
    {
        var teamStats = new TeamStats
        {
            Team = CreateTeam(1),
            GameOdds = new List<GameOdds>
            {
                CreateGameOdds(CreatePlayedGame(1, 2, Winner.AWAY, PeriodType.Overtime)),
            },
        };

        var result = TeamToTeamVmMapper.Map(teamStats);

        result.SeasonWins.Should().Be(0);
        result.SeasonLosses.Should().Be(0);
        result.SeasonOvertimeLosses.Should().Be(1);
    }

    [TestMethod]
    public void Map_ShouldCalculateAverageLogLoss()
    {
        var teamStats = new TeamStats
        {
            Team = CreateTeam(1),
            GameOdds = new List<GameOdds>
            {
                CreateGameOdds(CreatePlayedGame(1, 2, Winner.HOME), logLoss: 0.4),
                CreateGameOdds(CreatePlayedGame(1, 3, Winner.HOME), logLoss: 0.6),
            },
        };

        var result = TeamToTeamVmMapper.Map(teamStats);

        result.ModelLogLoss.Should().BeApproximately(0.5, 0.001);
    }

    [TestMethod]
    public void Map_WithCorrectPredictions_ShouldCountAccurate()
    {
        // Model predicts home (0.6 > 0.5), home actually wins
        var teamStats = new TeamStats
        {
            Team = CreateTeam(1),
            GameOdds = new List<GameOdds>
            {
                CreateGameOdds(CreatePlayedGame(1, 2, Winner.HOME), homeOdds: 0.6, awayOdds: 0.4),
                CreateGameOdds(CreatePlayedGame(1, 3, Winner.AWAY), homeOdds: 0.6, awayOdds: 0.4), // wrong prediction
            },
        };

        var result = TeamToTeamVmMapper.Map(teamStats);

        result.TotalModelAccurateGameCount.Should().Be(1);
    }

    [TestMethod]
    public void Map_WithDraftKingsOdds_ShouldCalculateDkStats()
    {
        var game = CreatePlayedGame(1, 2, Winner.HOME);
        var gameOdds = CreateGameOdds(game);
        gameOdds.BookmakerOdds = new List<BookmakerGameOdds>
        {
            new BookmakerGameOdds { BookmakerName = "DraftKings", HomeOdds = 0.6, AwayOdds = 0.4 },
        };

        var teamStats = new TeamStats
        {
            Team = CreateTeam(1),
            GameOdds = new List<GameOdds> { gameOdds },
        };

        var result = TeamToTeamVmMapper.Map(teamStats);

        result.DraftKingsGameCount.Should().Be(1);
        result.DraftKingsAccurateGameCount.Should().Be(1);
        result.DraftKingsLogLoss.Should().BeGreaterThan(0);
    }

    [TestMethod]
    public void Map_WithNoDraftKingsOdds_ShouldHaveZeroDkStats()
    {
        var game = CreatePlayedGame(1, 2, Winner.HOME);
        var gameOdds = CreateGameOdds(game);
        gameOdds.BookmakerOdds = new List<BookmakerGameOdds>
        {
            new BookmakerGameOdds { BookmakerName = "FanDuel", HomeOdds = 0.6, AwayOdds = 0.4 },
        };

        var teamStats = new TeamStats
        {
            Team = CreateTeam(1),
            GameOdds = new List<GameOdds> { gameOdds },
        };

        var result = TeamToTeamVmMapper.Map(teamStats);

        result.DraftKingsGameCount.Should().Be(0);
        result.DraftKingsAccurateGameCount.Should().Be(0);
        result.DraftKingsLogLoss.Should().Be(0);
    }
}
