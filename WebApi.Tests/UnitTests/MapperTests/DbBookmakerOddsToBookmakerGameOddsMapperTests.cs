using DatabaseAccess.WebBookmakerOddsRepository.Mappers;
using Entities.DbModels;
using FluentAssertions;

namespace WebApi.Tests.BusinessLogic.UnitTests.MapperTests;

[TestClass]
public class DbBookmakerOddsToBookmakerGameOddsMapperTests
{
    [TestMethod]
    public void Map_WithOddsOnly_ShouldMapBaseProperties()
    {
        var dbOdds = new DbBookmakerOdds
        {
            GameId = 2024020001,
            BookmakerName = "DraftKings",
            HomeOdds = 0.55,
            AwayOdds = 0.45,
        };

        var result = DbBookmakerOddsToBookmakerGameOddsMapper.Map(dbOdds, null, null);

        result.BookmakerName.Should().Be("DraftKings");
        result.HomeOdds.Should().Be(0.55);
        result.AwayOdds.Should().Be(0.45);
        result.HomePoint.Should().Be(0);
        result.OverUnderPoint.Should().Be(0);
    }

    [TestMethod]
    public void Map_WithSpreads_ShouldMapSpreadProperties()
    {
        var dbOdds = new DbBookmakerOdds
        {
            GameId = 2024020001,
            BookmakerName = "DraftKings",
            HomeOdds = 0.55,
            AwayOdds = 0.45,
        };
        var dbSpread = new DbBookmakerSpreads
        {
            GameId = 2024020001,
            BookmakerName = "DraftKings",
            HomePoint = -1.5,
            HomePrice = -110,
            AwayPoint = 1.5,
            AwayPrice = -110,
        };

        var result = DbBookmakerOddsToBookmakerGameOddsMapper.Map(dbOdds, dbSpread, null);

        result.HomePoint.Should().Be(-1.5);
        result.HomePrice.Should().Be(-110);
        result.AwayPoint.Should().Be(1.5);
        result.AwayPrice.Should().Be(-110);
    }

    [TestMethod]
    public void Map_WithTotals_ShouldMapTotalProperties()
    {
        var dbOdds = new DbBookmakerOdds
        {
            GameId = 2024020001,
            BookmakerName = "FanDuel",
            HomeOdds = 0.50,
            AwayOdds = 0.50,
        };
        var dbTotal = new DbBookmakerTotals
        {
            GameId = 2024020001,
            BookmakerName = "FanDuel",
            OverUnderPoint = 6.5,
            OverPrice = -115,
            UnderPrice = -105,
        };

        var result = DbBookmakerOddsToBookmakerGameOddsMapper.Map(dbOdds, null, dbTotal);

        result.OverUnderPoint.Should().Be(6.5);
        result.OverPrice.Should().Be(-115);
        result.UnderPrice.Should().Be(-105);
    }

    [TestMethod]
    public void Map_WithAllData_ShouldMapEverything()
    {
        var dbOdds = new DbBookmakerOdds
        {
            GameId = 2024020001,
            BookmakerName = "BetMGM",
            HomeOdds = 0.60,
            AwayOdds = 0.40,
        };
        var dbSpread = new DbBookmakerSpreads
        {
            HomePoint = -1.5,
            HomePrice = -120,
            AwayPoint = 1.5,
            AwayPrice = 100,
        };
        var dbTotal = new DbBookmakerTotals
        {
            OverUnderPoint = 5.5,
            OverPrice = -110,
            UnderPrice = -110,
        };

        var result = DbBookmakerOddsToBookmakerGameOddsMapper.Map(dbOdds, dbSpread, dbTotal);

        result.BookmakerName.Should().Be("BetMGM");
        result.HomeOdds.Should().Be(0.60);
        result.AwayOdds.Should().Be(0.40);
        result.HomePoint.Should().Be(-1.5);
        result.HomePrice.Should().Be(-120);
        result.OverUnderPoint.Should().Be(5.5);
        result.OverPrice.Should().Be(-110);
    }
}