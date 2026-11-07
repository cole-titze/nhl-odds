using Entities.Models.Web;
using FluentAssertions;
using WebApi.Mappers;

namespace WebApi.Tests.BusinessLogic.UnitTests.MapperTests;

[TestClass]
public class PredictionFactorsToVmMapperTests
{
    [TestMethod]
    public void Map_KeepsTheFactorsInRankOrder()
    {
        var runDate = new DateTime(2026, 11, 7, 11, 0, 0, DateTimeKind.Utc);
        var factors = new PredictionFactors
        {
            GameId = 2026020150,
            RunDateUTC = runDate,
            BaseHomeOdds = 0.54,
            Factors =
            [
                new PredictionFactor { Factor = "Rest", Detail = "Home 2 days since last game, away back-to-back", Contribution = 0.031 },
                new PredictionFactor { Factor = "Goaltending", Detail = "Save % this season: home .905, away .918", Contribution = -0.012 },
            ],
        };

        var vm = PredictionFactorsToVmMapper.Map(factors);

        vm.GameId.Should().Be(2026020150);
        vm.RunDateUTC.Should().Be(runDate);
        vm.BaseHomeOdds.Should().Be(0.54);
        vm.Factors.Select(f => f.Factor).Should().Equal("Rest", "Goaltending");
        vm.Factors.First().Detail.Should().Be("Home 2 days since last game, away back-to-back");
        vm.Factors.Last().Contribution.Should().Be(-0.012);
    }
}
