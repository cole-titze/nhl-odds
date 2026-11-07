using FluentAssertions;
using Search;
using WebApi.BusinessLogic.SearchGetter;

namespace WebApi.Tests.BusinessLogic.UnitTests.SearchTests;

[TestClass]
public class SearchGetterTests
{
    private const string EmptyResponse = """{"responses":[{"hits":{"hits":[]}},{"hits":{"hits":[]}},{"hits":{"hits":[]}},{"hits":{"hits":[]}}]}""";

    private sealed class CountingEmbedder : IQueryEmbedder
    {
        public int Calls { get; private set; }
        public double MinSimilarity => 0.5;

        public Task<float[]?> TryEmbed(string text, CancellationToken ct)
        {
            Calls++;
            return Task.FromResult<float[]?>([0.1f, 0.2f]);
        }
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<string> Bodies { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Bodies.Add(await request.Content!.ReadAsStringAsync(ct));
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent(EmptyResponse) };
        }
    }

    private static (SearchGetter Getter, RecordingHandler Handler, CountingEmbedder Embedder) Create()
    {
        var handler = new RecordingHandler();
        var client = new ElasticsearchClient(new HttpClient(handler), new ElasticsearchSettings("http://es:9200", null, null));
        var embedder = new CountingEmbedder();
        return (new SearchGetter(client, SearchGetter.CreateCache(), TimeProvider.System, embedder), handler, embedder);
    }

    [TestMethod]
    public async Task Suggest_SearchesThreeIndices_WithoutEmbeddingTheQuery()
    {
        var (getter, handler, embedder) = Create();

        var results = await getter.Suggest(" Lea ", CancellationToken.None);

        results.Query.Should().Be("Lea");
        embedder.Calls.Should().Be(0);
        handler.Bodies.Should().ContainSingle().Which.Split('\n', StringSplitOptions.RemoveEmptyEntries).Should().HaveCount(6);
    }

    [TestMethod]
    public async Task Suggest_CachesApartFromFullSearches()
    {
        var (getter, handler, _) = Create();

        await getter.Suggest("leafs", CancellationToken.None);
        await getter.Suggest("LEAFS", CancellationToken.None);
        await getter.Search("leafs", CancellationToken.None);

        // The second suggestion is cached; the full search for the same text isn't answered by it
        handler.Bodies.Should().HaveCount(2);
    }
}
