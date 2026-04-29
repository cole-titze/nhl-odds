using DatabaseAccess.GameRepository;
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
public class NhlGameManagerTests
{
    private IGameRepository _gameRepo = null!;
    private IPlayerRepository _playerRepo = null!;
    private INhlGameGetter _gameGetter = null!;
    private INhlPlayerGetter _playerGetter = null!;
    private INhlScheduleGetter _scheduleGetter = null!;
    private ILoggerFactory _loggerFactory = null!;
    private NhlGameManager _sut = null!;

    [TestInitialize]
    public void Setup()
    {
        _gameRepo = A.Fake<IGameRepository>();
        _playerRepo = A.Fake<IPlayerRepository>();
        _gameGetter = A.Fake<INhlGameGetter>();
        _playerGetter = A.Fake<INhlPlayerGetter>();
        _scheduleGetter = A.Fake<INhlScheduleGetter>();
        _loggerFactory = A.Fake<ILoggerFactory>();
        A.CallTo(() => _loggerFactory.CreateLogger(A<string>.Ignored)).Returns(A.Fake<ILogger>());

        var nhlDataGetter = new NhlApiDataGetter(_gameGetter, _playerGetter, _scheduleGetter);
        _sut = new NhlGameManager(_gameRepo, _playerRepo, nhlDataGetter, _loggerFactory);
    }

    [TestMethod]
    public async Task GetSeasonGameCount_WhenExistsInDb_ShouldReturnExistingCount()
    {
        A.CallTo(() => _gameRepo.GetGameCountForSeason(2024)).Returns(1312);

        var result = await _sut.GetSeasonGameCount(2024, ModeType.NhlAdd);

        result.Should().Be(1312);
        A.CallTo(() => _scheduleGetter.GetGameCountInSeason(A<int>.Ignored)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task GetSeasonGameCount_WhenUpdateMode_ShouldFetchFromApi()
    {
        A.CallTo(() => _gameRepo.GetGameCountForSeason(2024)).Returns(1312);
        A.CallTo(() => _scheduleGetter.GetGameCountInSeason(2024)).Returns(1320);

        var result = await _sut.GetSeasonGameCount(2024, ModeType.NhlUpdate);

        result.Should().Be(1320);
    }

    [TestMethod]
    public async Task GetSeasonGameCount_WhenNotInDb_ShouldFetchFromApi()
    {
        A.CallTo(() => _gameRepo.GetGameCountForSeason(2024)).Returns((int?)null);
        A.CallTo(() => _scheduleGetter.GetGameCountInSeason(2024)).Returns(1312);

        var result = await _sut.GetSeasonGameCount(2024, ModeType.NhlAdd);

        result.Should().Be(1312);
    }

    [TestMethod]
    public async Task GetGame_WhenExistsAndPlayed_InAddMode_ShouldReturnExistingGame()
    {
        var existingGame = new Game { Id = 2024020001, HasBeenPlayed = true };
        A.CallTo(() => _gameRepo.GetGame(2024020001)).Returns(existingGame);

        var result = await _sut.GetGame(2024020001, ModeType.NhlAdd);

        result.Should().NotBeNull();
        result!.Id.Should().Be(2024020001);
        A.CallTo(() => _gameGetter.GetGame(A<int>.Ignored)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task GetGame_WhenExistsAndPlayed_InUpdateMode_ShouldFetchFromApi()
    {
        var existingGame = new Game { Id = 2024020001, HasBeenPlayed = true };
        A.CallTo(() => _gameRepo.GetGame(2024020001)).Returns(existingGame);

        var freshGame = new Game { Id = 2024020001, HasBeenPlayed = true };
        A.CallTo(() => _gameGetter.GetGame(2024020001)).Returns(freshGame);
        A.CallTo(() => _playerGetter.BuildGameRosterStats(freshGame))
            .Returns(new GameRosterStats());

        var result = await _sut.GetGame(2024020001, ModeType.NhlUpdate);

        result.Should().NotBeNull();
        A.CallTo(() => _gameGetter.GetGame(2024020001)).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task GetGame_WhenNotAvailable_ShouldReturnNull()
    {
        A.CallTo(() => _gameRepo.GetGame(2024020999)).Returns((Game?)null);
        A.CallTo(() => _gameGetter.GetGame(2024020999)).Returns((Game?)null);

        var result = await _sut.GetGame(2024020999, ModeType.NhlAdd);

        result.Should().BeNull();
    }

    [TestMethod]
    public async Task GetGame_WhenExistsButNotPlayed_InAddMode_ShouldFetchFromApi()
    {
        var existingGame = new Game { Id = 2024020001, HasBeenPlayed = false };
        A.CallTo(() => _gameRepo.GetGame(2024020001)).Returns(existingGame);

        var freshGame = new Game { Id = 2024020001, HasBeenPlayed = false };
        A.CallTo(() => _gameGetter.GetGame(2024020001)).Returns(freshGame);
        A.CallTo(() => _playerGetter.BuildGameRosterStats(freshGame))
            .Returns(new GameRosterStats());

        var result = await _sut.GetGame(2024020001, ModeType.NhlAdd);

        result.Should().NotBeNull();
        A.CallTo(() => _gameGetter.GetGame(2024020001)).MustHaveHappenedOnceExactly();
    }
}