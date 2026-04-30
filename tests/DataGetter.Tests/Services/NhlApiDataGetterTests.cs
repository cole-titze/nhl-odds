using Entities.Types.Enums;
using FluentAssertions;
using Services.NhlData;

namespace DataGetter.Tests.Services;

[TestClass]
public class NhlApiDataGetterTests
{
    [TestMethod]
    public void GetGameId_BuildsRegularSeasonId()
    {
        NhlApiDataGetter.GetGameId(2024, 1).Should().Be(2024020001);
        NhlApiDataGetter.GetGameId(2024, 1312).Should().Be(2024021312);
    }

    [TestMethod]
    public void GetPlayoffGameId_BuildsPlayoffId()
    {
        NhlApiDataGetter.GetPlayoffGameId(2024, 1).Should().Be(2024030001);
        NhlApiDataGetter.GetPlayoffGameId(2024, 87).Should().Be(2024030087);
    }

    [TestMethod]
    public void GetGameTypeFromId_ReturnsRegularForType02()
    {
        NhlApiDataGetter.GetGameTypeFromId(2024020001).Should().Be(GameType.Regular);
        NhlApiDataGetter.GetGameTypeFromId(2009021200).Should().Be(GameType.Regular);
    }

    [TestMethod]
    public void GetGameTypeFromId_ReturnsPlayoffForType03()
    {
        NhlApiDataGetter.GetGameTypeFromId(2024030001).Should().Be(GameType.Playoff);
        NhlApiDataGetter.GetGameTypeFromId(2009030412).Should().Be(GameType.Playoff);
    }
}
