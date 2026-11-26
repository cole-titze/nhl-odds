using Entities.Models.Web;
using FluentAssertions;
using WebApi.Mappers;

namespace WebApi.Tests.BusinessLogic.UnitTests.MapperTests;

[TestClass]
public class ErrorLogToErrorLogVmMapperTests
{
    [TestMethod]
    public void Map_ShouldMapAllProperties()
    {
        var errorLog = new ErrorLog
        {
            Id = 42,
            TimestampUTC = new DateTime(2024, 3, 10, 14, 30, 0),
            GameId = 2024020500,
            SeasonStartYear = 2024,
            ExceptionType = "HttpRequestException",
            Message = "Connection refused",
            StackTrace = "at Services.NhlApi.GetGame()",
            Source = "Services",
        };

        var result = ErrorLogToErrorLogVmMapper.Map(errorLog);

        result.Id.Should().Be(42);
        result.TimestampUTC.Should().Be(new DateTime(2024, 3, 10, 14, 30, 0));
        result.GameId.Should().Be(2024020500);
        result.SeasonStartYear.Should().Be(2024);
        result.ExceptionType.Should().Be("HttpRequestException");
        result.Message.Should().Be("Connection refused");
        result.StackTrace.Should().Be("at Services.NhlApi.GetGame()");
        result.Source.Should().Be("Services");
    }

    [TestMethod]
    public void Map_WithNullOptionalFields_ShouldMapNulls()
    {
        var errorLog = new ErrorLog
        {
            Id = 1,
            TimestampUTC = new DateTime(2024, 1, 1),
            GameId = null,
            SeasonStartYear = null,
            ExceptionType = "Exception",
            Message = "Something broke",
        };

        var result = ErrorLogToErrorLogVmMapper.Map(errorLog);

        result.GameId.Should().BeNull();
        result.SeasonStartYear.Should().BeNull();
    }
}
