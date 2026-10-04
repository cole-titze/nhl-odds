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
    public void GetAllPlayoffGameIds_NormalSeason_HasRounds1To4Only()
    {
        var ids = NhlApiDataGetter.GetAllPlayoffGameIds(2024).ToList();

        ids.Should().HaveCount(15 * 7);
        ids.Should().NotContain(id => id % 10000 < 100);
        ids.Should().Contain([2024030111, 2024030187, 2024030417]);
    }

    [TestMethod]
    public void GetAllPlayoffGameIds_BubbleSeason_IncludesRound0()
    {
        var ids = NhlApiDataGetter.GetAllPlayoffGameIds(2019).ToList();

        ids.Should().HaveCount(25 * 7);
        // Round-robin groups (series 0-1) and qualifying series (2-9)
        ids.Should().Contain([2019030001, 2019030016, 2019030021, 2019030094, 2019030111, 2019030417]);
        ids.Should().OnlyHaveUniqueItems();
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
