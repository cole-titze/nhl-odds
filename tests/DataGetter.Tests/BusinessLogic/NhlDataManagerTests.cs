using DatabaseAccess.BroadcasterRepository;
using DatabaseAccess.ErrorRepository;
using DatabaseAccess.GameEventRepository;
using DatabaseAccess.GameRepository;
using DatabaseAccess.PlayerRepository;
using DatabaseAccess.TeamRepository;
using DataGetter.BusinessLogic;
using Entities.Models;
using Entities.Models.Teams;
using Entities.Types;
using Entities.Types.Enums;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Services.NhlData;

namespace DataGetter.Tests.BusinessLogic;

[TestClass]
public class NhlDataManagerTests
{
    private IGameRepository _gameRepo = null!;
    private IPlayerRepository _playerRepo = null!;
    private ITeamRepository _teamRepo = null!;
    private IErrorRepository _errorRepo = null!;
    private IBroadcasterRepository _broadcasterRepo = null!;
    private IGameEventRepository _gameEventRepo = null!;
    private NhlApiDataGetter _nhlDataGetter = null!;
    private ILoggerFactory _loggerFactory = null!;

    [TestInitialize]
    public void Setup()
    {
        _gameRepo = A.Fake<IGameRepository>();
        _playerRepo = A.Fake<IPlayerRepository>();
        _teamRepo = A.Fake<ITeamRepository>();
        _errorRepo = A.Fake<IErrorRepository>();
        _broadcasterRepo = A.Fake<IBroadcasterRepository>();
        _gameEventRepo = A.Fake<IGameEventRepository>();
        _nhlDataGetter = A.Fake<NhlApiDataGetter>();
        _loggerFactory = A.Fake<ILoggerFactory>();
        A.CallTo(() => _loggerFactory.CreateLogger(A<string>.Ignored)).Returns(A.Fake<ILogger>());

        // Force the playoff segment in FetchAndSaveSeasonData to terminate. Without these,
        // FakeItEasy creates dummy Game instances for unconfigured Task<Game?> calls (Game has
        // a parameterless ctor), so consecutiveUnavailable never increments and the
        // knownCount=null playoff loop runs forever.
        A.CallTo(() => _gameRepo.GetGame(A<int>.Ignored)).Returns((Game?)null);
        A.CallTo(() => _nhlDataGetter.GameDataGetter.GetGame(A<int>.Ignored)).Returns((Game?)null);
    }

    private NhlDataManager CreateSut()
    {
        var gameManager = new NhlGameManager(_gameRepo, _playerRepo, _nhlDataGetter, _loggerFactory);
        var playerManager = new NhlPlayerManager(_playerRepo, _nhlDataGetter, _loggerFactory);
        var teamManager = new NhlTeamManager(_teamRepo, _nhlDataGetter, _loggerFactory);

        return new NhlDataManager(
            _gameRepo, _playerRepo, _teamRepo, _errorRepo, _broadcasterRepo, _gameEventRepo,
            gameManager, playerManager, teamManager, _loggerFactory);
    }

    [TestMethod]
    public async Task GetNhlData_WhenAllGamesExistAndNotCurrentYear_ShouldSkipSeason()
    {
        // 2020: has all games, is not current year -> skip
        A.CallTo(() => _gameRepo.GetSavedGameCountForSeason(2020)).Returns(1312);
        A.CallTo(() => _gameRepo.GetGameCountForSeason(2020)).Returns(1312);
        A.CallTo(() => _gameRepo.HasPlayoffGamesForSeason(2020)).Returns(true);

        // 2021: current year — needs team data
        A.CallTo(() => _gameRepo.GetSavedGameCountForSeason(2021)).Returns(1312);
        A.CallTo(() => _gameRepo.GetGameCountForSeason(2021)).Returns(1312);
        A.CallTo(() => _teamRepo.HasSeasonTeams(2021)).Returns(true);
        A.CallTo(() => _teamRepo.GetSeasonTeams(2021)).Returns(new List<Team>());

        var sut = CreateSut();
        await sut.GetNhlData(new YearRange(2020, 2021), ModeType.NhlAdd);

        // Season 2020 skipped — no team data fetched for it
        A.CallTo(() => _teamRepo.HasSeasonTeams(2020)).MustNotHaveHappened();
        // Season 2021 (current year) should NOT be skipped
        A.CallTo(() => _teamRepo.HasSeasonTeams(2021)).MustHaveHappened();
    }

    [TestMethod]
    public async Task GetNhlData_WhenModeIsUpdate_ShouldNotSkipSeason()
    {
        A.CallTo(() => _gameRepo.GetSavedGameCountForSeason(2020)).Returns(1312);
        A.CallTo(() => _gameRepo.GetGameCountForSeason(2020)).Returns(1312);
        A.CallTo(() => _teamRepo.HasSeasonTeams(2020)).Returns(true);
        A.CallTo(() => _teamRepo.GetSeasonTeams(2020)).Returns(new List<Team>());
        A.CallTo(() => _nhlDataGetter.ScheduleDataGetter.GetAllTeams()).Returns(new List<Team>());
        A.CallTo(() => _nhlDataGetter.ScheduleDataGetter.GetTeamsForSeason(2020)).Returns(new List<SeasonTeam>());
        A.CallTo(() => _nhlDataGetter.ScheduleDataGetter.GetGameCountInSeason(2020)).Returns(0);

        var sut = CreateSut();
        await sut.GetNhlData(new YearRange(2020, 2020), ModeType.NhlUpdate);

        // Update mode should never skip — team data should be fetched from API
        A.CallTo(() => _nhlDataGetter.ScheduleDataGetter.GetAllTeams()).MustHaveHappened();
    }

    [TestMethod]
    public async Task GetNhlData_WhenSeasonMissingGames_ShouldNotSkipSeason()
    {
        // 2020: missing games (saved=1000, total=1312) -> should NOT skip
        A.CallTo(() => _gameRepo.GetSavedGameCountForSeason(2020)).Returns(1000);
        A.CallTo(() => _gameRepo.GetGameCountForSeason(2020)).Returns(1312);
        A.CallTo(() => _teamRepo.HasSeasonTeams(2020)).Returns(true);
        A.CallTo(() => _teamRepo.GetSeasonTeams(2020)).Returns(new List<Team>());

        // 2021 current year
        A.CallTo(() => _gameRepo.GetSavedGameCountForSeason(2021)).Returns(0);
        A.CallTo(() => _gameRepo.GetGameCountForSeason(2021)).Returns((int?)null);
        A.CallTo(() => _teamRepo.HasSeasonTeams(2021)).Returns(true);
        A.CallTo(() => _teamRepo.GetSeasonTeams(2021)).Returns(new List<Team>());
        A.CallTo(() => _nhlDataGetter.ScheduleDataGetter.GetGameCountInSeason(2021)).Returns(0);

        var sut = CreateSut();
        await sut.GetNhlData(new YearRange(2020, 2021), ModeType.NhlAdd);

        // 2020 should NOT be skipped since it's missing games
        A.CallTo(() => _teamRepo.HasSeasonTeams(2020)).MustHaveHappened();
    }

}