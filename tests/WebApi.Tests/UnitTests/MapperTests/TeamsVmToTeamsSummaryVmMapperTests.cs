using Entities.ViewModels;
using FluentAssertions;
using WebApi.Mappers;

namespace WebApi.Tests.BusinessLogic.UnitTests.MapperTests;

[TestClass]
public class TeamsVmToTeamsSummaryVmMapperTests
{
    private static TeamsVM CreateTeamsVm() => new()
    {
        Teams = new List<TeamVM>
        {
            new()
            {
                Id = 1,
                LocationName = "New Jersey",
                TeamName = "Devils",
                LogoUri = "logo",
                ModelLogLoss = 0.61,
                TotalGameCount = 4,
                SeasonWins = 3,
                SeasonLosses = 1,
                SeasonOvertimeLosses = 0,
                TotalModelAccurateGameCount = 2,
                DraftKingsLogLoss = 0.65,
                DraftKingsAccurateGameCount = 1,
                DraftKingsGameCount = 4,
                GameOddsVM = new List<GameOddsVM> { new(), new() },
            },
        },
        SeasonTotals = new SeasonTotalsVM { TotalGameCount = 2, ModelLogLoss = 0.61 },
    };

    [TestMethod]
    public void Map_ShouldCopyTeamStatsAndSeasonTotals()
    {
        var teamsVm = CreateTeamsVm();

        var result = TeamsVmToTeamsSummaryVmMapper.Map(teamsVm);

        result.Teams.Should().ContainSingle().Which.Should().BeEquivalentTo(teamsVm.Teams.Single(),
            options => options.Excluding(t => t.GameOddsVM));
        result.SeasonTotals.Should().BeEquivalentTo(teamsVm.SeasonTotals);
    }

    [TestMethod]
    public void Map_ShouldNotChangeCachedTeamsVm()
    {
        var teamsVm = CreateTeamsVm();

        TeamsVmToTeamsSummaryVmMapper.Map(teamsVm);

        teamsVm.Teams.Single().GameOddsVM.Should().HaveCount(2);
    }
}
