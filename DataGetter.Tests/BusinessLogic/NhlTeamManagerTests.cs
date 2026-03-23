using DatabaseAccess.TeamRepository;
using DataGetter.BusinessLogic;
using Entities.Models.Teams;
using Entities.Types.Enums;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Services.NhlData;

namespace DataGetter.Tests.BusinessLogic;

[TestClass]
public class NhlTeamManagerTests
{
    private ITeamRepository _teamRepo = null!;
    private INhlScheduleGetter _scheduleGetter = null!;
    private ILoggerFactory _loggerFactory = null!;
    private NhlTeamManager _sut = null!;

    [TestInitialize]
    public void Setup()
    {
        _teamRepo = A.Fake<ITeamRepository>();
        _scheduleGetter = A.Fake<INhlScheduleGetter>();
        _loggerFactory = A.Fake<ILoggerFactory>();
        A.CallTo(() => _loggerFactory.CreateLogger(A<string>.Ignored)).Returns(A.Fake<ILogger>());

        var nhlDataGetter = new NhlApiDataGetter(
            A.Fake<INhlGameGetter>(),
            A.Fake<INhlPlayerGetter>(),
            _scheduleGetter);

        _sut = new NhlTeamManager(_teamRepo, nhlDataGetter, _loggerFactory);
    }

    [TestMethod]
    public async Task GetTeamData_WhenSeasonTeamsExist_InAddMode_ShouldReturnExisting()
    {
        var existingTeams = new List<Team>
        {
            new Team { Id = 1, Abbreviation = "BOS" },
        };
        A.CallTo(() => _teamRepo.HasSeasonTeams(2024)).Returns(true);
        A.CallTo(() => _teamRepo.GetSeasonTeams(2024)).Returns(existingTeams);

        var result = await _sut.GetTeamData(2024, ModeType.NhlAdd);

        result.Should().HaveCount(1);
        A.CallTo(() => _scheduleGetter.GetAllTeams()).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task GetTeamData_WhenSeasonTeamsExist_InUpdateMode_ShouldFetchFromApi()
    {
        A.CallTo(() => _teamRepo.HasSeasonTeams(2024)).Returns(true);
        A.CallTo(() => _teamRepo.GetSeasonTeams(2024)).Returns(new List<Team>());

        var apiTeams = new List<Team>
        {
            new Team { Id = 1, Abbreviation = "BOS" },
            new Team { Id = 2, Abbreviation = "MTL" },
        };
        A.CallTo(() => _scheduleGetter.GetAllTeams()).Returns(apiTeams);

        var seasonTeams = new List<SeasonTeam>
        {
            new SeasonTeam { SeasonStartYear = 2024, Abbreviation = "BOS", Name = "Bruins" },
            new SeasonTeam { SeasonStartYear = 2024, Abbreviation = "MTL", Name = "Canadiens" },
        };
        A.CallTo(() => _scheduleGetter.GetTeamsForSeason(2024)).Returns(seasonTeams);

        var result = (await _sut.GetTeamData(2024, ModeType.NhlUpdate)).ToList();

        result.Should().HaveCount(2);
        result.First(t => t.Abbreviation == "BOS").SeasonInformation.Should().ContainKey(2024);
    }

    [TestMethod]
    public async Task GetTeamData_ShouldDeduplicateTeamsByAbbreviation_KeepHighestId()
    {
        A.CallTo(() => _teamRepo.HasSeasonTeams(2024)).Returns(false);
        A.CallTo(() => _teamRepo.GetSeasonTeams(2024)).Returns(new List<Team>());

        var apiTeams = new List<Team>
        {
            new Team { Id = 50, Abbreviation = "UTA" },
            new Team { Id = 100, Abbreviation = "UTA" },
        };
        A.CallTo(() => _scheduleGetter.GetAllTeams()).Returns(apiTeams);

        var seasonTeams = new List<SeasonTeam>
        {
            new SeasonTeam { SeasonStartYear = 2024, Abbreviation = "UTA", Name = "Utah Hockey Club" },
        };
        A.CallTo(() => _scheduleGetter.GetTeamsForSeason(2024)).Returns(seasonTeams);

        var result = (await _sut.GetTeamData(2024, ModeType.NhlAdd)).ToList();

        result.Should().HaveCount(1);
        result[0].Id.Should().Be(100);
    }

    [TestMethod]
    public async Task GetTeamData_BuildTeams_ShouldPopulateSeasonInformation()
    {
        A.CallTo(() => _teamRepo.HasSeasonTeams(2024)).Returns(false);
        A.CallTo(() => _teamRepo.GetSeasonTeams(2024)).Returns(new List<Team>());

        var apiTeams = new List<Team>
        {
            new Team { Id = 1, Abbreviation = "BOS" },
        };
        A.CallTo(() => _scheduleGetter.GetAllTeams()).Returns(apiTeams);

        var seasonTeams = new List<SeasonTeam>
        {
            new SeasonTeam { SeasonStartYear = 2023, Abbreviation = "BOS", Name = "Bruins 23" },
            new SeasonTeam { SeasonStartYear = 2024, Abbreviation = "BOS", Name = "Bruins 24" },
        };
        A.CallTo(() => _scheduleGetter.GetTeamsForSeason(2024)).Returns(seasonTeams);

        var result = (await _sut.GetTeamData(2024, ModeType.NhlAdd)).ToList();

        result.Should().HaveCount(1);
        var team = result[0];
        team.SeasonInformation.Should().HaveCount(2);
        team.SeasonInformation[2023].Name.Should().Be("Bruins 23");
        team.SeasonInformation[2024].Name.Should().Be("Bruins 24");
    }
}
