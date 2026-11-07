using System.Net;
using System.Text.Json.Nodes;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Search;
using WebApi.BusinessLogic.SearchGetter;

namespace WebApi.Tests.BusinessLogic.UnitTests.SearchTests;

[TestClass]
public class VectorSearchTests
{
    private sealed class FakeEmbedder(string model = "nomic-embed-text", Exception? fail = null, int dims = 3) : IEmbedder
    {
        public string Model => model;
        public List<List<string>> Batches { get; } = new();

        public Task<float[][]> Embed(IReadOnlyList<string> texts, EmbeddingPurpose purpose, CancellationToken ct = default)
        {
            Batches.Add(texts.ToList());
            if (fail != null)
                throw fail;
            return Task.FromResult(texts.Select(_ => Enumerable.Repeat(0.5f, dims).ToArray()).ToArray());
        }
    }

    private static ArticleDocument Article(string id, string title, int day) =>
        new(id, title, null, "body of " + title, new DateTime(2026, 11, day, 0, 0, 0, DateTimeKind.Utc), "https://www.nhl.com/news/" + id);

    private static JsonNode Hit(string id) => new JsonObject
    {
        ["_id"] = id,
        ["_source"] = new JsonObject { ["title"] = id, ["url"] = "u/" + id },
        ["highlight"] = new JsonObject { ["body"] = new JsonArray(id + " snippet") },
    };

    private sealed class ManualTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
        public void Advance(TimeSpan by) => now += by;
    }

    [TestMethod]
    public void BuildRequest_WithAVector_AddsAKnnSearchOfTheArticles()
    {
        var lines = SiteSearch.BuildRequest(new ParsedQuery("who got traded", null), DateTime.UtcNow, [0.1f, 0.2f], 0.6)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);

        lines.Should().HaveCount(10);
        lines[8].Should().Be($"{{\"index\":\"{SearchIndexes.Articles.Alias}\"}}");
        var knn = JsonNode.Parse(lines[9])!;
        knn["knn"]!["field"]!.GetValue<string>().Should().Be("embedding");
        knn["knn"]!["similarity"]!.GetValue<double>().Should().Be(0.6);
        knn["knn"]!["query_vector"]!.AsArray().Should().HaveCount(2);
        knn["size"]!.GetValue<int>().Should().Be(SiteSearch.FusionDepth);
        // The keyword half goes as deep, so the fusion has more than a page of each
        JsonNode.Parse(lines[7])!["size"]!.GetValue<int>().Should().Be(SiteSearch.FusionDepth);
    }

    [TestMethod]
    public void BuildRequest_DateOnlyWithAVector_SkipsTheKnnSearch()
    {
        var lines = SiteSearch.BuildRequest(new ParsedQuery("", new DateOnly(2026, 11, 3)), DateTime.UtcNow, [0.1f])
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);

        lines.Should().HaveCount(8);
    }

    [TestMethod]
    public void FuseArticles_RanksAnArticleBothFindFirst()
    {
        var fused = SiteSearch.FuseArticles([Hit("a"), Hit("b"), Hit("c")], [Hit("c"), Hit("d")]);

        // c: 1/63 + 1/61 beats a's 1/61 alone; d (rank 2 by vector) ties b (rank 2 by keyword), keyword first
        fused.Select(h => h["_id"]!.GetValue<string>()).Should().Equal("c", "a", "b", "d");
    }

    [TestMethod]
    public void FuseArticles_NoVectorHits_KeepsTheKeywordOrder()
    {
        SiteSearch.FuseArticles([Hit("a"), Hit("b")], []).Select(h => h["_id"]!.GetValue<string>()).Should().Equal("a", "b");
    }

    [TestMethod]
    public void ReadResponse_AFailedKnnSearch_StillGivesKeywordArticles()
    {
        var response = JsonNode.Parse("""
            {"responses":[
              {"hits":{"hits":[]}}, {"hits":{"hits":[]}}, {"hits":{"hits":[]}},
              {"hits":{"hits":[{"_id":"a","_source":{"title":"A","url":"u"}}]}},
              {"error":{"type":"search_phase_execution_exception","reason":"field [embedding] does not exist"},"status":400}
            ]}
            """)!;

        SiteSearch.ReadResponse("trade", response).Articles.Should().ContainSingle().Which.Title.Should().Be("A");
    }

    [TestMethod]
    public void ReadResponse_FusedArticles_StopAtAPage()
    {
        var keyword = new JsonArray(Enumerable.Range(0, 10).Select(i => (JsonNode)Hit($"k{i}")).ToArray());
        var vector = new JsonArray(Enumerable.Range(0, 10).Select(i => (JsonNode)Hit($"v{i}")).ToArray());
        var empty = new JsonObject { ["hits"] = new JsonObject { ["hits"] = new JsonArray() } };
        var response = new JsonObject
        {
            ["responses"] = new JsonArray(empty.DeepClone(), empty.DeepClone(), empty.DeepClone(),
                new JsonObject { ["hits"] = new JsonObject { ["hits"] = keyword } },
                new JsonObject { ["hits"] = new JsonObject { ["hits"] = vector } }),
        };

        SiteSearch.ReadResponse("q", response).Articles.Should().HaveCount(SearchIndexes.Articles.Size);
    }

    [TestMethod]
    public async Task Add_ReusesAnUnchangedArticlesVector_AndEmbedsTheRestNewestFirst()
    {
        var embedder = new FakeEmbedder();
        var unchanged = Article("old", "Same", 1);
        var key = ArticleEmbeddings.EmbeddingKey(embedder.Model, ArticleEmbeddings.EmbeddingText(unchanged));
        var previous = new Dictionary<string, PreviousEmbedding>
        {
            ["old"] = new(key, [0.9f, 0.1f, 0.1f]),
            // Its text changed since, so the stored vector is stale
            ["edited"] = new("some-other-key", [0.9f, 0.1f, 0.1f]),
        };

        var result = await new ArticleEmbeddings(embedder, NullLogger.Instance).Add(
            [unchanged, Article("edited", "Edited", 2), Article("new", "New", 3)], previous, TimeSpan.FromMinutes(1));

        result.Single(a => a.EntityId == "old").Embedding.Should().Equal(0.9f, 0.1f, 0.1f);
        result.Single(a => a.EntityId == "edited").Embedding.Should().Equal(0.5f, 0.5f, 0.5f);
        result.Should().OnlyContain(a => a.EmbeddingKey != null);
        embedder.Batches.Should().ContainSingle().Which.Should().HaveCount(2)
            .And.Subject.First().Should().StartWith("New");
    }

    [TestMethod]
    public async Task Add_ADifferentModel_EmbedsAgain()
    {
        var article = Article("a", "A", 1);
        var oldKey = ArticleEmbeddings.EmbeddingKey("all-minilm", ArticleEmbeddings.EmbeddingText(article));
        var embedder = new FakeEmbedder("nomic-embed-text");

        var result = await new ArticleEmbeddings(embedder, NullLogger.Instance).Add(
            [article], new Dictionary<string, PreviousEmbedding> { ["a"] = new(oldKey, [1f]) }, TimeSpan.FromMinutes(1));

        embedder.Batches.Should().ContainSingle();
        result[0].Embedding.Should().HaveCount(3);
    }

    [TestMethod]
    public async Task Add_ServerDown_IndexesWithoutVectorsInsteadOfFailing()
    {
        var embedder = new FakeEmbedder(fail: new HttpRequestException("connection refused"));
        var articles = Enumerable.Range(1, 40).Select(i => Article($"a{i}", $"A{i}", 1 + i % 28)).ToList();

        var result = await new ArticleEmbeddings(embedder, NullLogger.Instance).Add(articles, new Dictionary<string, PreviousEmbedding>(), TimeSpan.FromMinutes(1));

        result.Should().HaveCount(40).And.OnlyContain(a => a.Embedding == null);
        embedder.Batches.Should().ContainSingle("it stops at the first failure rather than timing out each batch");
    }

    [TestMethod]
    public async Task Add_MixedSizes_KeepsTheNewestSize()
    {
        var embedder = new FakeEmbedder(dims: 4);
        var old = Article("old", "Old", 1);
        var key = ArticleEmbeddings.EmbeddingKey(embedder.Model, ArticleEmbeddings.EmbeddingText(old));

        var result = await new ArticleEmbeddings(embedder, NullLogger.Instance).Add(
            [old, Article("new", "New", 5)], new Dictionary<string, PreviousEmbedding> { ["old"] = new(key, [1f, 0f, 0f]) },
            TimeSpan.FromMinutes(1));

        result.Single(a => a.EntityId == "new").Embedding.Should().HaveCount(4);
        result.Single(a => a.EntityId == "old").Embedding.Should().BeNull();
    }

    [TestMethod]
    public void EmbeddingKey_ChangesWithTheModelsPrefix()
    {
        // nomic-embed-text gets a "search_document: " prefix, all-minilm none; same text, different vectors
        EmbeddingPrompts.Prefix("nomic-embed-text:v1.5", EmbeddingPurpose.Document).Should().NotBeEmpty();
        ArticleEmbeddings.EmbeddingKey("nomic-embed-text:v1.5", "text")
            .Should().NotBe(ArticleEmbeddings.EmbeddingKey("nomic-embed-text:v1.5", "other text"))
            .And.NotBe(ArticleEmbeddings.EmbeddingKey("all-minilm", "text"));
    }

    [TestMethod]
    public async Task Add_SendsSmallBatchesOneAtATime()
    {
        var embedder = new FakeEmbedder();
        var articles = Enumerable.Range(1, 10).Select(i => Article($"a{i}", $"A{i}", i)).ToList();

        await new ArticleEmbeddings(embedder, NullLogger.Instance).Add(articles, new Dictionary<string, PreviousEmbedding>(), TimeSpan.FromMinutes(1));

        embedder.Batches.Select(b => b.Count).Should().Equal(4, 4, 2);
    }

    [TestMethod]
    public void EmbeddingText_KeepsTheOpeningOfALongArticle()
    {
        var article = Article("a", "Title", 1) with { Body = new string('x', 10_000) };

        ArticleEmbeddings.EmbeddingText(article).Should().StartWith("Title\n").And.HaveLength(ArticleEmbeddings.MaxTextChars);
    }

    [DataTestMethod]
    [DataRow("nomic-embed-text", "search_query: ")]
    [DataRow("nomic-embed-text:v1.5", "search_query: ")]
    [DataRow("embeddinggemma:300m", "task: search result | query: ")]
    [DataRow("all-minilm", "")]
    public void Prefix_DependsOnTheModel(string model, string prefix)
    {
        EmbeddingPrompts.Prefix(model, EmbeddingPurpose.Query).Should().Be(prefix);
    }

    [TestMethod]
    public void Settings_UnsetUrl_TurnsVectorSearchOff()
    {
        EmbeddingSettings.From(_ => null).Should().BeNull();
        EmbeddingSettings.From(k => k == EmbeddingSettings.UrlKey ? "http://e:11434/" : null)
            .Should().Be(new EmbeddingSettings("http://e:11434", "nomic-embed-text:v1.5", 0.55));
    }

    [TestMethod]
    public async Task QueryEmbedder_AFailure_SkipsTheServerForAMinute()
    {
        var time = new ManualTime(DateTimeOffset.UtcNow);
        var embedder = new FakeEmbedder(fail: new HttpRequestException("down"));
        var settings = new EmbeddingSettings("http://e", "nomic-embed-text", 0.55);
        var query = new QueryEmbedder(embedder, settings, new EmbeddingCircuit(time), NullLogger<QueryEmbedder>.Instance);

        (await query.TryEmbed("trade", CancellationToken.None)).Should().BeNull();
        (await query.TryEmbed("trade", CancellationToken.None)).Should().BeNull();
        embedder.Batches.Should().HaveCount(1);

        time.Advance(EmbeddingCircuit.Pause + TimeSpan.FromSeconds(1));
        await query.TryEmbed("trade", CancellationToken.None);
        embedder.Batches.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task OllamaEmbedder_SendsThePrefixedBatch_AndReadsTheVectors()
    {
        var handler = new RecordingHandler("""{"model":"nomic-embed-text","embeddings":[[0.1,0.2],[0.3,0.4]]}""");
        var embedder = new OllamaEmbedder(new HttpClient(handler), new EmbeddingSettings("http://ollama:11434", "nomic-embed-text", 0.55));

        var vectors = await embedder.Embed(["a", "b"], EmbeddingPurpose.Document);

        vectors.Should().HaveCount(2);
        vectors[1].Should().Equal(0.3f, 0.4f);
        handler.Uri.Should().Be("http://ollama:11434/api/embed");
        var body = JsonNode.Parse(handler.Body!)!;
        body["input"]![0]!.GetValue<string>().Should().Be("search_document: a");
        body["truncate"]!.GetValue<bool>().Should().BeTrue();
    }

    [TestMethod]
    public async Task OllamaEmbedder_ModelNotPulled_Throws()
    {
        var handler = new RecordingHandler("""{"error":"model \"nomic-embed-text\" not found"}""", HttpStatusCode.NotFound);
        var embedder = new OllamaEmbedder(new HttpClient(handler), new EmbeddingSettings("http://ollama:11434", "nomic-embed-text", 0.55));

        var act = () => embedder.Embed(["a"], EmbeddingPurpose.Query);

        await act.Should().ThrowAsync<EmbeddingException>().WithMessage("*not found*");
    }

    private sealed class RecordingHandler(string response, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        public string? Uri { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Uri = request.RequestUri!.ToString();
            Body = await request.Content!.ReadAsStringAsync(ct);
            return new HttpResponseMessage(status) { Content = new StringContent(response) };
        }
    }
}
