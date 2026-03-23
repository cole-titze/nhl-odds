using Entities.Models.Web;
using Entities.Types;
using Entities.Types.Enums;
using FluentAssertions;
using WebApi.Mappers;

namespace WebApi.Tests.BusinessLogic.UnitTests.MapperTests;

[TestClass]
public class GameOddsToViewModelsMapperTests
{
    [TestMethod]
    public void Map_WithEmptyList_ShouldReturnEmptyList()
    {
        var result = GameOddsToViewModelsMapper.Map(new List<GameOdds>());

        result.Should().BeEmpty();
    }

    [TestMethod]
    public void Map_ShouldMapGameProperties()
    {
        var gameOdds = new GameOdds
        {
            ModelHomeOdds = 0.55,
            ModelAwayOdds = 0.45,
            LogLoss = 0.6,
            ModelId = 1,
            PredictedSpread = -1.5,
            SpreadCoverProb = 0.55,
            PredictedTotal = 6.0,
            TotalOverProb = 0.48,
            Game = new Game
            {
                Id = 2024020100,
                GameDate = new DateTime(2024, 1, 15),
                HomeGoals = 3,
                AwayGoals = 2,
                Winner = Winner.HOME,
                HasBeenPlayed = true,
                EndPeriod = PeriodType.Regulation,
                HomeTeam = new Team { Id = 1, LocationName = "Boston", TeamName = "Bruins", LogoUri = "bruins.png" },
                AwayTeam = new Team { Id = 2, LocationName = "Montreal", TeamName = "Canadiens", LogoUri = "habs.png" },
            },
        };

        var result = GameOddsToViewModelsMapper.Map(new List<GameOdds> { gameOdds }).ToList();

        result.Should().HaveCount(1);
        var vm = result[0];
        vm.Id.Should().Be(2024020100);
        vm.GameDate.Should().Be(new DateTime(2024, 1, 15));
        vm.Winner.Should().Be(Winner.HOME);
        vm.HasBeenPlayed.Should().BeTrue();
        vm.LogLoss.Should().Be(0.6);
        vm.ModelId.Should().Be(1);
        vm.PredictedSpread.Should().Be(-1.5);
        vm.SpreadCoverProb.Should().Be(0.55);
        vm.PredictedTotal.Should().Be(6.0);
        vm.TotalOverProb.Should().Be(0.48);
    }

    [TestMethod]
    public void Map_ShouldMapHomeTeamCorrectly()
    {
        var gameOdds = new GameOdds
        {
            ModelHomeOdds = 0.6,
            ModelAwayOdds = 0.4,
            Game = new Game
            {
                HomeTeam = new Team { Id = 10, LocationName = "Tampa Bay", TeamName = "Lightning", LogoUri = "tb.png" },
                AwayTeam = new Team { Id = 20, LocationName = "Florida", TeamName = "Panthers", LogoUri = "fla.png" },
                HomeGoals = 4,
                AwayGoals = 1,
            },
        };

        var result = GameOddsToViewModelsMapper.Map(new List<GameOdds> { gameOdds }).ToList();
        var home = result[0].HomeTeam!;

        home.Id.Should().Be(10);
        home.LocationName.Should().Be("Tampa Bay");
        home.TeamName.Should().Be("Lightning");
        home.LogoUri.Should().Be("tb.png");
        home.ModelOdds.Should().Be(0.6);
        home.Goals.Should().Be(4);
        home.Team.Should().Be(Winner.HOME);
    }

    [TestMethod]
    public void Map_ShouldMapAwayTeamCorrectly()
    {
        var gameOdds = new GameOdds
        {
            ModelHomeOdds = 0.6,
            ModelAwayOdds = 0.4,
            Game = new Game
            {
                HomeTeam = new Team { Id = 10 },
                AwayTeam = new Team { Id = 20, LocationName = "Florida", TeamName = "Panthers", LogoUri = "fla.png" },
                HomeGoals = 4,
                AwayGoals = 1,
            },
        };

        var result = GameOddsToViewModelsMapper.Map(new List<GameOdds> { gameOdds }).ToList();
        var away = result[0].AwayTeam!;

        away.Id.Should().Be(20);
        away.LocationName.Should().Be("Florida");
        away.ModelOdds.Should().Be(0.4);
        away.Goals.Should().Be(1);
        away.Team.Should().Be(Winner.AWAY);
    }

    [TestMethod]
    public void Map_ShouldMapBookmakerOdds()
    {
        var gameOdds = new GameOdds
        {
            Game = new Game
            {
                HomeTeam = new Team { Id = 1 },
                AwayTeam = new Team { Id = 2 },
            },
            BookmakerOdds = new List<BookmakerGameOdds>
            {
                new BookmakerGameOdds
                {
                    BookmakerName = "DraftKings",
                    HomeOdds = 0.55,
                    AwayOdds = 0.45,
                    HomePoint = -1.5,
                    HomePrice = -110,
                    AwayPoint = 1.5,
                    AwayPrice = -110,
                    OverUnderPoint = 6.0,
                    OverPrice = -115,
                    UnderPrice = -105,
                },
            },
        };

        var result = GameOddsToViewModelsMapper.Map(new List<GameOdds> { gameOdds }).ToList();
        var bk = result[0].BookmakerOdds;

        bk.Should().HaveCount(1);
        bk[0].BookmakerName.Should().Be("DraftKings");
        bk[0].HomeOdds.Should().Be(0.55);
        bk[0].AwayOdds.Should().Be(0.45);
        bk[0].HomePoint.Should().Be(-1.5);
        bk[0].HomePrice.Should().Be(-110);
        bk[0].OverUnderPoint.Should().Be(6.0);
    }
}
