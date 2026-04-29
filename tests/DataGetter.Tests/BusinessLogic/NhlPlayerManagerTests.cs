using DatabaseAccess.PlayerRepository;
using DataGetter.BusinessLogic;
using Entities.Models;
using Entities.Types.Enums;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Services.NhlData;

namespace DataGetter.Tests.BusinessLogic;

[TestClass]
public class NhlPlayerManagerTests
{
    private IPlayerRepository _playerRepo = null!;
    private INhlPlayerGetter _playerGetter = null!;
    private ILoggerFactory _loggerFactory = null!;
    private NhlPlayerManager _sut = null!;

    [TestInitialize]
    public void Setup()
    {
        _playerRepo = A.Fake<IPlayerRepository>();
        _playerGetter = A.Fake<INhlPlayerGetter>();
        _loggerFactory = A.Fake<ILoggerFactory>();
        A.CallTo(() => _loggerFactory.CreateLogger(A<string>.Ignored)).Returns(A.Fake<ILogger>());

        var nhlDataGetter = new NhlApiDataGetter(
            A.Fake<INhlGameGetter>(),
            _playerGetter,
            A.Fake<INhlScheduleGetter>());

        _sut = new NhlPlayerManager(_playerRepo, nhlDataGetter, _loggerFactory);
    }

    [TestMethod]
    public async Task GetPlayers_WhenPlayerExists_InAddMode_ShouldSkipPlayer()
    {
        var game = new Game
        {
            Id = 2024020001,
            HomeTeamId = 1,
            AwayTeamId = 2,
            RosterStats = new GameRosterStats
            {
                HomeTeamForwards = new List<GameSkaterStats> { new GameSkaterStats { PlayerId = 101 } },
            },
        };

        A.CallTo(() => _playerRepo.GetPlayer(101)).Returns(new Player { Id = 101 });

        var result = (await _sut.GetPlayers(game, ModeType.NhlAdd)).ToList();

        A.CallTo(() => _playerGetter.GetPlayer(101, A<int>.Ignored)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task GetPlayers_WhenPlayerExists_InUpdateMode_ShouldFetchPlayer()
    {
        var game = new Game
        {
            Id = 2024020001,
            HomeTeamId = 1,
            AwayTeamId = 2,
            RosterStats = new GameRosterStats
            {
                HomeTeamForwards = new List<GameSkaterStats> { new GameSkaterStats { PlayerId = 101 } },
            },
        };

        A.CallTo(() => _playerRepo.GetPlayer(101)).Returns(new Player { Id = 101 });
        A.CallTo(() => _playerGetter.GetPlayer(101, 1)).Returns(new Player { Id = 101 });

        var result = (await _sut.GetPlayers(game, ModeType.NhlUpdate)).ToList();

        A.CallTo(() => _playerGetter.GetPlayer(101, 1)).MustHaveHappenedOnceExactly();
        result.Should().HaveCount(1);
    }

    [TestMethod]
    public async Task GetPlayers_WhenPlayerDoesNotExist_InAddMode_ShouldFetchPlayer()
    {
        var game = new Game
        {
            Id = 2024020001,
            HomeTeamId = 1,
            AwayTeamId = 2,
            RosterStats = new GameRosterStats
            {
                AwayTeamForwards = new List<GameSkaterStats> { new GameSkaterStats { PlayerId = 202 } },
            },
        };

        A.CallTo(() => _playerRepo.GetPlayer(202)).Returns((Player?)null);
        A.CallTo(() => _playerGetter.GetPlayer(202, 2)).Returns(new Player { Id = 202 });

        var result = (await _sut.GetPlayers(game, ModeType.NhlAdd)).ToList();

        A.CallTo(() => _playerGetter.GetPlayer(202, 2)).MustHaveHappenedOnceExactly();
        result.Should().HaveCount(1);
    }

    [TestMethod]
    public async Task GetPlayers_WithNullRosterStats_ShouldReturnEmpty()
    {
        var game = new Game
        {
            Id = 2024020001,
            HomeTeamId = 1,
            AwayTeamId = 2,
            RosterStats = null,
        };

        var result = (await _sut.GetPlayers(game, ModeType.NhlAdd)).ToList();

        result.Should().BeEmpty();
    }
}