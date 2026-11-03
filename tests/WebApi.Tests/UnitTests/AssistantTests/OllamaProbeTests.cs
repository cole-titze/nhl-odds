using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WebApi.BusinessLogic.Assistant;

namespace WebApi.Tests.BusinessLogic.UnitTests.AssistantTests;

[TestClass]
public class OllamaProbeTests
{
    // Counts requests and answers each with the current reply (null throws, like an unreachable host)
    private sealed class FakeOllama : HttpMessageHandler
    {
        public int Calls;
        public HttpStatusCode? Status = HttpStatusCode.OK;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            request.RequestUri!.AbsolutePath.Should().Be("/api/version");
            return Status is { } status
                ? Task.FromResult(new HttpResponseMessage(status))
                : throw new HttpRequestException("No route to host");
        }
    }

    private sealed class FakeTimeProvider : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 11, 3, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan by) => _now += by;
    }

    private static OllamaProbe Create(FakeOllama ollama, TimeProvider time) =>
        new(new HttpClient(ollama) { BaseAddress = new Uri("http://ollama/") }, NullLogger<OllamaProbe>.Instance, time);

    [TestMethod]
    public async Task IsReachable_ReusesAnUpAnswerUntilItExpires()
    {
        var ollama = new FakeOllama();
        var time = new FakeTimeProvider();
        var probe = Create(ollama, time);

        (await probe.IsReachable()).Should().BeTrue();
        ollama.Status = null;
        time.Advance(OllamaProbe.UpFor - TimeSpan.FromSeconds(1));
        (await probe.IsReachable()).Should().BeTrue();
        ollama.Calls.Should().Be(1);

        time.Advance(TimeSpan.FromSeconds(1));
        (await probe.IsReachable()).Should().BeFalse();
        ollama.Calls.Should().Be(2);
    }

    [TestMethod]
    public async Task IsReachable_RechecksSoonerWhenDown()
    {
        var ollama = new FakeOllama { Status = null };
        var time = new FakeTimeProvider();
        var probe = Create(ollama, time);

        (await probe.IsReachable()).Should().BeFalse();
        ollama.Status = HttpStatusCode.OK;
        time.Advance(OllamaProbe.DownFor);
        (await probe.IsReachable()).Should().BeTrue();
    }

    [TestMethod]
    public async Task IsReachable_ErrorStatusIsDown()
    {
        var probe = Create(new FakeOllama { Status = HttpStatusCode.InternalServerError }, new FakeTimeProvider());
        (await probe.IsReachable()).Should().BeFalse();
    }
}
