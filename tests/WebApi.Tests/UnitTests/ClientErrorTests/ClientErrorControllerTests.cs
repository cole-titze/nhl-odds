using System.Threading.RateLimiting;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging.Abstractions;
using WebApi.BusinessLogic.ClientErrors;
using WebApi.Controllers;
using WebApi.Tests.BusinessLogic.Fakes;

namespace WebApi.Tests.BusinessLogic.UnitTests.ClientErrorTests;

[TestClass]
public class ClientErrorControllerTests
{
    private static readonly ClientErrorReport ValidReport =
        new("error", "TypeError", "x is undefined", "at f (app.js:1:2)", "/game/2026020123");

    private static ClientErrorController Controller(FakeErrorLogRepository repo, ClientErrorThrottle? throttle = null) =>
        new(repo, throttle ?? new ClientErrorThrottle(), NullLogger<ClientErrorController>.Instance);

    private static ClientErrorThrottle ThrottleWith(int tokens, int dailyCap = 100, TimeProvider? time = null) =>
        new(new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            TokenLimit = tokens,
            ReplenishmentPeriod = TimeSpan.FromHours(1),
            TokensPerPeriod = 1,
            AutoReplenishment = false,
            QueueLimit = 0,
        }), dailyCap, time ?? TimeProvider.System);

    private static ClientErrorReport Report(int i) => ValidReport with { Message = $"error {i}" };

    [TestMethod]
    public async Task Post_ValidReport_SavesFrontendErrorAndReturnsNoContent()
    {
        var repo = new FakeErrorLogRepository();

        var result = await Controller(repo).Post(ValidReport, "Mozilla/5.0", CancellationToken.None);

        result.Should().BeOfType<NoContent>();
        repo.Added.Should().ContainSingle();
        repo.Added[0].Source.Should().Be("Frontend");
        repo.Added[0].ExceptionType.Should().Be("TypeError");
        repo.Added[0].Message.Should().Be("x is undefined");
    }

    [DataTestMethod]
    [DataRow("api")]
    [DataRow("chunk")]
    public async Task Post_ApiAndChunkReports_AreSaved(string kind)
    {
        var repo = new FakeErrorLogRepository();

        var result = await Controller(repo).Post(
            new ClientErrorReport(kind, "ApiError", "GET /api/Team/GetTeam answered 500", null, "/team/10", "2026.11.03-abc1234"),
            null, CancellationToken.None);

        result.Should().BeOfType<NoContent>();
        repo.Added.Should().ContainSingle().Which.StackTrace.Should().StartWith($"Kind: {kind}\nVersion: 2026.11.03-abc1234\n");
    }

    [DataTestMethod]
    [DataRow("bogus", "msg", "/")]
    [DataRow("error", "", "/")]
    [DataRow("error", "   ", "/")]
    [DataRow("error", "msg", "https://evil.example/")]
    public async Task Post_InvalidReport_ReturnsBadRequestAndSavesNothing(string kind, string message, string page)
    {
        var repo = new FakeErrorLogRepository();

        var result = await Controller(repo).Post(new ClientErrorReport(kind, null, message, null, page), null, CancellationToken.None);

        result.Should().BeOfType<BadRequest>();
        repo.Added.Should().BeEmpty();
    }

    [TestMethod]
    public async Task Post_OverSharedRate_DropsReportButStillReturnsNoContent()
    {
        var repo = new FakeErrorLogRepository();
        var controller = Controller(repo, ThrottleWith(2));

        for (var i = 0; i < 3; i++)
            (await controller.Post(Report(i), null, CancellationToken.None)).Should().BeOfType<NoContent>();

        repo.Added.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task Post_OverDailyCap_DropsUntilTheNextUtcDay()
    {
        var repo = new FakeErrorLogRepository();
        var time = new FakeTime(new DateTimeOffset(2026, 11, 3, 23, 0, 0, TimeSpan.Zero));
        var controller = Controller(repo, ThrottleWith(100, dailyCap: 3, time));

        for (var i = 0; i < 5; i++)
            await controller.Post(Report(i), null, CancellationToken.None);
        repo.Added.Should().HaveCount(3);

        time.Now = time.Now.AddHours(2);
        await controller.Post(Report(99), null, CancellationToken.None);
        repo.Added.Should().HaveCount(4);
    }

    [TestMethod]
    public async Task Post_SameErrorAgain_SavedOncePerWindow()
    {
        var repo = new FakeErrorLogRepository();
        var time = new FakeTime(new DateTimeOffset(2026, 11, 3, 12, 0, 0, TimeSpan.Zero));
        var controller = Controller(repo, ThrottleWith(100, time: time));

        await controller.Post(ValidReport, null, CancellationToken.None);
        await controller.Post(ValidReport with { Page = "/teams", Stack = "other" }, null, CancellationToken.None);
        repo.Added.Should().HaveCount(1);

        time.Now = time.Now.AddMinutes(ClientErrorLimits.DuplicateWindowMinutes);
        await controller.Post(ValidReport, null, CancellationToken.None);
        repo.Added.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task Post_DuplicateDoesNotUseRateTokens()
    {
        var repo = new FakeErrorLogRepository();
        var controller = Controller(repo, ThrottleWith(2));

        for (var i = 0; i < 5; i++)
            await controller.Post(ValidReport, null, CancellationToken.None);
        await controller.Post(Report(1), null, CancellationToken.None);

        repo.Added.Should().HaveCount(2);
    }

    private sealed class FakeTime(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;
        public override DateTimeOffset GetUtcNow() => Now;
    }

    [TestMethod]
    public async Task Post_DatabaseFails_ReturnsNoContent()
    {
        var repo = new FakeErrorLogRepository(new InvalidOperationException("db down"));

        var result = await Controller(repo).Post(ValidReport, null, CancellationToken.None);

        result.Should().BeOfType<NoContent>();
    }
}
