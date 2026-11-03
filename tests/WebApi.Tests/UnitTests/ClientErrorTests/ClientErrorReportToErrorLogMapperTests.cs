using FluentAssertions;
using WebApi.BusinessLogic.ClientErrors;
using WebApi.Mappers;

namespace WebApi.Tests.BusinessLogic.UnitTests.ClientErrorTests;

[TestClass]
public class ClientErrorReportToErrorLogMapperTests
{
    private static readonly DateTime Now = new(2026, 11, 3, 18, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public void Map_PutsKindVersionPageAndUserAgentAboveTheStack()
    {
        var report = new ClientErrorReport("render", "TypeError", "boom", "at a\nat b", "/teams", "2026.11.03-abc1234");

        var log = ClientErrorReportToErrorLogMapper.Map(report, "Mozilla/5.0 (iPhone)", Now);

        log.TimestampUTC.Should().Be(Now);
        log.Source.Should().Be("Frontend");
        log.ExceptionType.Should().Be("TypeError");
        log.Message.Should().Be("boom");
        log.StackTrace.Should().Be("Kind: render\nVersion: 2026.11.03-abc1234\nPage: /teams\nUser agent: Mozilla/5.0 (iPhone)\n\nat a\nat b");
        log.GameId.Should().BeNull();
    }

    [TestMethod]
    public void Map_NoNameOrStack_UsesErrorAndOnlyTheDetails()
    {
        var log = ClientErrorReportToErrorLogMapper.Map(new ClientErrorReport("error", " ", "boom", null, null), null, Now);

        log.ExceptionType.Should().Be("Error");
        log.StackTrace.Should().Be("Kind: error\nVersion: unknown\nPage: \nUser agent: ");
    }

    [DataTestMethod]
    [DataRow(2026, 9, 19, 2025)]
    [DataRow(2026, 9, 20, 2026)]
    [DataRow(2027, 3, 1, 2026)]
    public void Map_FilesUnderTheCurrentSeason(int year, int month, int day, int expectedSeason)
    {
        var log = ClientErrorReportToErrorLogMapper.Map(
            new ClientErrorReport("error", null, "boom", null, "/"), null, new DateTime(year, month, day, 12, 0, 0, DateTimeKind.Utc));

        log.SeasonStartYear.Should().Be(expectedSeason);
    }

    [TestMethod]
    public void Map_CutsLongFieldsShort()
    {
        var report = new ClientErrorReport("error", new string('n', 500), new string('m', 5_000), new string('s', 20_000), "/" + new string('p', 500));

        var log = ClientErrorReportToErrorLogMapper.Map(report, new string('u', 1_000), Now);

        log.ExceptionType.Length.Should().Be(ClientErrorLimits.MaxNameChars);
        log.Message.Length.Should().Be(ClientErrorLimits.MaxMessageChars);
        log.Message.Should().EndWith("…");
        log.StackTrace.Length.Should().Be(ClientErrorLimits.MaxStackTraceChars);
        log.StackTrace.Should().Contain("Page: /" + new string('p', ClientErrorLimits.MaxPageChars - 2) + "…\n");
        log.StackTrace.Should().Contain("User agent: " + new string('u', ClientErrorLimits.MaxUserAgentChars - 1) + "…\n");
    }
}
