using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using WebApi.BusinessLogic.Assistant;
using WebApi.BusinessLogic.GameOddsGetter;
using WebApi.BusinessLogic.TeamGetter;
using WebApi.Caching;
using WebApi.McpTools;

namespace WebApi.Tests.BusinessLogic.UnitTests.AssistantTests;

[TestClass]
public class AssistantGateTests
{
    // Answers each Ollama /api/chat call with the next queued reply and keeps the request bodies
    private sealed class FakeOllama(params Func<HttpResponseMessage>[] replies) : HttpMessageHandler
    {
        private int _next;
        public List<JsonObject> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(JsonNode.Parse(await request.Content!.ReadAsStringAsync(cancellationToken))!.AsObject());
            return replies[Math.Min(_next++, replies.Length - 1)]();
        }
    }

    private static Func<HttpResponseMessage> Reply(string content) => () => new HttpResponseMessage(HttpStatusCode.OK)
    {
        Content = new StringContent(
            new JsonObject { ["message"] = new JsonObject { ["role"] = "assistant", ["content"] = content } }.ToJsonString(),
            Encoding.UTF8, "application/json"),
    };

    private static HttpClient Client(FakeOllama ollama) => new(ollama) { BaseAddress = new Uri("http://ollama") };

    private static List<AssistantMessage> Ask(string question) => [new("user", question)];

    private static AssistantService CreateService(FakeOllama ollama, Dictionary<string, string?>? settings = null)
    {
        var services = new ServiceCollection()
            .AddSingleton(A.Fake<ITeamGetter>())
            .AddSingleton(A.Fake<IGameOddsGetter>())
            .AddKeyedSingleton(ApiCache.ServiceKey, (_, _) => ApiCache.Create())
            .BuildServiceProvider();
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings ?? []).Build();
        return new AssistantService(Client(ollama), config,
            ActivatorUtilities.CreateInstance<GameOddsMcpTools>(services),
            ActivatorUtilities.CreateInstance<TeamMcpTools>(services),
            services.GetRequiredService<IGameOddsGetter>(),
            NullLogger<AssistantService>.Instance);
    }

    [TestMethod]
    [DataRow("ON", GateResult.On)]
    [DataRow(" off.", GateResult.Off)]
    [DataRow("On\n", GateResult.On)]
    [DataRow("ONE", GateResult.Error)]
    [DataRow("The answer is 391", GateResult.Error)]
    [DataRow("", GateResult.Error)]
    [DataRow(null, GateResult.Error)]
    public void Parse_ShouldOnlyAcceptAClearVerdict(string? reply, GateResult expected) =>
        AssistantGate.Parse(reply).Should().Be(expected);

    [TestMethod]
    public async Task Check_ShouldSendAShortToollessCall()
    {
        var ollama = new FakeOllama(Reply("ON"));

        var result = await AssistantGate.Check(Client(ollama), "gate-model", 2048, Ask("Who wins tonight?"),
            NullLogger.Instance, CancellationToken.None);

        result.Should().Be(GateResult.On);
        var body = ollama.Requests.Single();
        body["model"]!.GetValue<string>().Should().Be("gate-model");
        body.ContainsKey("tools").Should().BeFalse();
        body["think"]!.GetValue<bool>().Should().BeFalse();
        body["options"]!["num_predict"]!.GetValue<int>().Should().BeLessThanOrEqualTo(8);
        body["messages"]![1]!["content"]!.GetValue<string>().Should().Contain("Who wins tonight?");
    }

    [TestMethod]
    public async Task Check_FollowUp_ShouldIncludeThePreviousReply()
    {
        var ollama = new FakeOllama(Reply("ON"));
        List<AssistantMessage> messages =
        [
            new("user", "How are the Oilers doing?"),
            new("assistant", "Edmonton is 8-3 this season."),
            new("user", "and the Leafs?"),
        ];

        await AssistantGate.Check(Client(ollama), "m", 2048, messages, NullLogger.Instance, CancellationToken.None);

        var prompt = ollama.Requests.Single()["messages"]![1]!["content"]!.GetValue<string>();
        prompt.Should().Contain("Edmonton is 8-3").And.Contain("and the Leafs?");
        prompt.Should().NotContain("How are the Oilers doing?");
    }

    [TestMethod]
    public async Task Check_ServerError_ShouldLetTheQuestionThrough()
    {
        var ollama = new FakeOllama(() => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var result = await AssistantGate.Check(Client(ollama), "m", 2048, Ask("hi"), NullLogger.Instance, CancellationToken.None);

        result.Should().Be(GateResult.Error);
    }

    [TestMethod]
    public async Task Chat_OffTopic_ShouldNotRunTheMainModel()
    {
        var ollama = new FakeOllama(Reply("OFF"), Reply("391"));

        var reply = await CreateService(ollama).Chat(new AssistantRequest(Ask("What is 17*23?"), "/"), CancellationToken.None);

        reply.Should().Be(AssistantGate.OffTopicReply);
        ollama.Requests.Should().ContainSingle();
    }

    [TestMethod]
    public async Task Chat_OnTopic_ShouldAnswerWithTheMainModel()
    {
        var ollama = new FakeOllama(Reply("ON"), Reply("The Oilers are favored."));

        var reply = await CreateService(ollama).Chat(new AssistantRequest(Ask("Who wins tonight?"), "/"), CancellationToken.None);

        reply.Should().Be("The Oilers are favored.");
        ollama.Requests.Should().HaveCount(2);
        ollama.Requests[1].ContainsKey("tools").Should().BeTrue();
    }

    [TestMethod]
    public async Task Chat_UnclearVerdict_ShouldAnswerWithTheMainModel()
    {
        var ollama = new FakeOllama(Reply("Sure! 17*23 is 391"), Reply("answer"));

        var reply = await CreateService(ollama).Chat(new AssistantRequest(Ask("hmm"), "/"), CancellationToken.None);

        reply.Should().Be("answer");
    }

    [TestMethod]
    public async Task Chat_GateModelSetting_ShouldOnlyChangeTheGateModel()
    {
        var ollama = new FakeOllama(Reply("ON"), Reply("answer"));
        var settings = new Dictionary<string, string?> { ["OLLAMA_MODEL"] = "big", ["OLLAMA_GATE_MODEL"] = "small" };

        await CreateService(ollama, settings).Chat(new AssistantRequest(Ask("hi"), "/"), CancellationToken.None);

        ollama.Requests[0]["model"]!.GetValue<string>().Should().Be("small");
        ollama.Requests[1]["model"]!.GetValue<string>().Should().Be("big");        ollama.Requests[0]["options"]!["num_ctx"]!.GetValue<int>().Should().Be(AssistantGate.ContextTokens);
    }

    [TestMethod]
    public async Task Chat_SameModel_ShouldAskForTheSameContextSoOllamaDoesNotReload()
    {
        var ollama = new FakeOllama(Reply("ON"), Reply("answer"));

        await CreateService(ollama).Chat(new AssistantRequest(Ask("hi"), "/"), CancellationToken.None);

        ollama.Requests[0]["options"]!["num_ctx"]!.GetValue<int>()
            .Should().Be(ollama.Requests[1]["options"]!["num_ctx"]!.GetValue<int>());
    }

    [TestMethod]
    public async Task Chat_ShouldKeepTheModelLoadedOnEveryCall()
    {
        var ollama = new FakeOllama(Reply("ON"), Reply("answer"));

        await CreateService(ollama).Chat(new AssistantRequest(Ask("hi"), "/"), CancellationToken.None);

        ollama.Requests.Should().HaveCount(2).And.OnlyContain(r => r["keep_alive"]!.GetValue<int>() == AssistantLimits.KeepAlive);
    }
}
