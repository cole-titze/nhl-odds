using Entities.Models.Web;
using FluentAssertions;
using WebApi.BusinessLogic.AdminService;
using WebApi.Tests.BusinessLogic.Fakes;

namespace WebApi.Tests.BusinessLogic.UnitTests.AdminServiceTests;

[TestClass]
public class AdminServiceUnitTests
{
    [TestMethod]
    public async Task GetErrorLogs_WithNoErrors_ShouldReturnEmptyList()
    {
        var repo = new FakeAdminRepository();
        var service = new AdminService(repo);

        var result = await service.GetErrorLogs(null);

        result.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetErrorLogs_WithErrors_ShouldReturnMappedViewModels()
    {
        var errorLogs = new List<ErrorLog>
        {
            new ErrorLog
            {
                Id = 1,
                TimestampUTC = new DateTime(2024, 1, 1),
                GameId = 2024020001,
                SeasonStartYear = 2024,
                ExceptionType = "HttpRequestException",
                Message = "Connection refused",
                StackTrace = "at Some.Method()",
                Source = "Services",
            },
            new ErrorLog
            {
                Id = 2,
                TimestampUTC = new DateTime(2024, 1, 2),
                SeasonStartYear = 2024,
                ExceptionType = "TimeoutException",
                Message = "Request timed out",
                StackTrace = "at Another.Method()",
                Source = "DataGetter",
            },
        };
        var repo = new FakeAdminRepository(errorLogs: errorLogs);
        var service = new AdminService(repo);

        var result = await service.GetErrorLogs(null);

        result.Should().HaveCount(2);
        result[0].Id.Should().Be(1);
        result[0].ExceptionType.Should().Be("HttpRequestException");
        result[0].GameId.Should().Be(2024020001);
        result[1].Id.Should().Be(2);
        result[1].Message.Should().Be("Request timed out");
    }

    [TestMethod]
    public async Task GetErrorLogs_WithSeasonFilter_ShouldFilterBySeason()
    {
        var errorLogs = new List<ErrorLog>
        {
            new ErrorLog { Id = 1, SeasonStartYear = 2023, Message = "Error 2023" },
            new ErrorLog { Id = 2, SeasonStartYear = 2024, Message = "Error 2024" },
        };
        var repo = new FakeAdminRepository(errorLogs: errorLogs);
        var service = new AdminService(repo);

        var result = await service.GetErrorLogs(2024);

        result.Should().HaveCount(1);
        result[0].SeasonStartYear.Should().Be(2024);
    }

    [TestMethod]
    public async Task GetHealthChecks_WithNoData_ShouldReturnEmptyList()
    {
        var repo = new FakeAdminRepository();
        var service = new AdminService(repo);

        var result = await service.GetHealthChecks();

        result.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetHealthChecks_WithData_ShouldReturnMappedViewModels()
    {
        var healthChecks = new List<SeasonHealthCheck>
        {
            new SeasonHealthCheck
            {
                SeasonStartYear = 2024,
                TotalGames = 1312,
                PlayedGames = 800,
                MissingPredictions = 5,
                MissingBookmakerOdds = 3,
                MissingGameCleaned = 10,
                MissingOddsFetchDays = 2,
                LiveBookmakerOdds = 15,
                ErrorCount = 7,
            },
        };
        var repo = new FakeAdminRepository(healthChecks: healthChecks);
        var service = new AdminService(repo);

        var result = await service.GetHealthChecks();

        result.Should().HaveCount(1);
        result[0].SeasonStartYear.Should().Be(2024);
        result[0].TotalGames.Should().Be(1312);
        result[0].PlayedGames.Should().Be(800);
        result[0].MissingPredictions.Should().Be(5);
        result[0].ErrorCount.Should().Be(7);
    }
}
