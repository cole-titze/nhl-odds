using Entities.Types;
using Entities.ViewModels;
using FluentAssertions;
using WebApi.Mappers;

namespace WebApi.Tests.BusinessLogic.UnitTests.MapperTests;

[TestClass]
public class TeamsVmToSeasonTeamsVmMapperTests
{
    private static GameOddsVM Game(int id) => new()
    {
        Id = id,
        GameDate = new DateTime(2026, 10, id),
        HasBeenPlayed = true,
        LogLoss = 0.6,
        Winner = Winner.HOME,
        HomeTeam = new MatchupTeamVM { Id = 1 },
        AwayTeam = new MatchupTeamVM { Id = 2 },
        BookmakerOdds = [new BookmakerOddsVM { BookmakerName = "Kalshi", HomeOdds = 0.55, AwayOdds = 0.45, HomePrice = -120 }],
    };

    [TestMethod]
    public void Map_ShouldListEachGameOnceWithTeamIdsAndOdds()
    {
        var shared = Game(1);
        var teamsVm = new TeamsVM
        {
            Teams =
            [
                new TeamVM { Id = 1, TeamName = "Devils", GameOddsVM = [shared, Game(2)] },
                new TeamVM { Id = 2, TeamName = "Rangers", GameOddsVM = [shared] },
            ],
            SeasonTotals = new SeasonTotalsVM { TotalGameCount = 2 },
        };

        var result = TeamsVmToSeasonTeamsVmMapper.Map(teamsVm);

        result.Teams.Select(t => t.Id).Should().Equal(1, 2);
        result.SeasonTotals.TotalGameCount.Should().Be(2);
        result.Games.Select(g => g.Id).Should().Equal(1, 2);
        var game = result.Games.First();
        game.HomeTeamId.Should().Be(1);
        game.AwayTeamId.Should().Be(2);
        game.LogLoss.Should().Be(0.6);
        game.BookmakerOdds.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new SeasonTeamGameOddsVM { BookmakerName = "Kalshi", HomeOdds = 0.55, AwayOdds = 0.45 });
    }
}