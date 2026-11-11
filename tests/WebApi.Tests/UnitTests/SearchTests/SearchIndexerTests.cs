using System.Net;
using System.Text.Json.Nodes;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Search;

namespace WebApi.Tests.BusinessLogic.UnitTests.SearchTests;

[TestClass]
public class SearchIndexerTests
{
    private sealed record Doc(string Id, string Name);

    private static readonly SearchIndex Index = new("nhl-test", new JsonObject
    {
        ["properties"] = new JsonObject { ["name"] = new JsonObject { ["type"] = "text" } },
    }, Size: 5);

    private static readonly Doc[] Docs = [new("1", "Oilers"), new("2", "Flames")];
    private static readonly DateTime Now = new(2026, 11, 11, 20, 20, 0, DateTimeKind.Utc);

    [TestMethod]
    public void ContentHash_IgnoresDocumentOrder()
    {
        SearchIndexer.ContentHash(Index, Docs, d => d.Id)
            .Should().Be(SearchIndexer.ContentHash(Index, Docs.Reverse(), d => d.Id));
    }

    [TestMethod]
    public void ContentHash_ChangesWithADocumentOrTheMapping()
    {
        var hash = SearchIndexer.ContentHash(Index, Docs, d => d.Id);

        SearchIndexer.ContentHash(Index, [Docs[0], Docs[1] with { Name = "Canucks" }], d => d.Id).Should().NotBe(hash);
        SearchIndexer.ContentHash(Index, [Docs[0]], d => d.Id).Should().NotBe(hash);
        SearchIndexer.ContentHash(Index.WithProperty("extra", new JsonObject { ["type"] = "keyword" }), Docs, d => d.Id)
            .Should().NotBe(hash);
    }

    [TestMethod]
    public async Task Rebuild_SkipsWhenTheLiveIndexHasTheSameContent()
    {
        var hash = SearchIndexer.ContentHash(Index, Docs, d => d.Id);
        var handler = new FakeElasticsearch { LiveHash = hash };

        var rebuilt = await Indexer(handler).Rebuild(Index, Docs, d => d.Id, Now);

        rebuilt.Should().BeFalse();
        handler.Requests.Should().Equal("GET nhl-test/_mapping");
    }

    [TestMethod]
    public async Task Rebuild_BuildsWithoutReplicaThenTurnsItOnBeforeMovingTheAlias()
    {
        var handler = new FakeElasticsearch { LiveHash = "something-else" };

        var rebuilt = await Indexer(handler).Rebuild(Index, Docs, d => d.Id, Now);

        rebuilt.Should().BeTrue();
        var name = "nhl-test-20261111202000";
        handler.Requests.Should().ContainInOrder(
            "GET nhl-test/_mapping",
            $"PUT {name}",
            "POST _bulk",
            $"POST {name}/_refresh",
            $"PUT {name}/_settings",
            $"GET {name}/_stats/docs",
            "POST _aliases");

        var create = handler.Bodies[$"PUT {name}"]!;
        create["settings"]!["number_of_replicas"]!.GetValue<int>().Should().Be(0);
        create["settings"]!["refresh_interval"]!.GetValue<string>().Should().Be("-1");
        create["mappings"]!["_meta"]![SearchIndex.ContentHashKey]!.GetValue<string>()
            .Should().Be(SearchIndexer.ContentHash(Index, Docs, d => d.Id));

        var live = handler.Bodies[$"PUT {name}/_settings"]!["index"]!;
        live["auto_expand_replicas"]!.GetValue<string>().Should().Be("0-1");
        live.AsObject().ContainsKey("refresh_interval").Should().BeTrue();
        live["refresh_interval"].Should().BeNull();
    }

    [TestMethod]
    public async Task Rebuild_WaitsForTheReplicaToStart()
    {
        var handler = new FakeElasticsearch { LiveHash = null, StartedCopies = [1, 1, 2] };

        await Indexer(handler).Rebuild(Index, Docs, d => d.Id, Now);

        handler.Requests.Count(r => r.EndsWith("/_stats/docs")).Should().Be(3);
    }

    private static SearchIndexer Indexer(FakeElasticsearch handler) =>
        new(new ElasticsearchClient(new HttpClient(handler) { BaseAddress = new Uri("http://es:9200/") },
            new ElasticsearchSettings("http://es:9200", null, null)), NullLogger<SearchIndexer>.Instance);

    /// <summary>Answers the calls a rebuild makes; the new index has 1 replica, started after StartedCopies polls.</summary>
    private sealed class FakeElasticsearch : HttpMessageHandler
    {
        public string? LiveHash { get; init; }
        public int[] StartedCopies { get; init; } = [2];
        public List<string> Requests { get; } = new();
        public Dictionary<string, JsonNode?> Bodies { get; } = new();
        private int _statsCalls;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var path = request.RequestUri!.AbsolutePath.TrimStart('/');
            var key = $"{request.Method} {path}";
            Requests.Add(key);
            if (request.Content != null && request.Content.Headers.ContentType?.MediaType == "application/json")
                Bodies[key] = JsonNode.Parse(await request.Content.ReadAsStringAsync(ct));

            var index = path.Split('/')[0];
            string body = key switch
            {
                "GET nhl-test/_mapping" => LiveHash == null
                    ? "{}"
                    : """{"nhl-test-old":{"mappings":{"_meta":{"content_hash":""" + $"\"{LiveHash}\"" + "}}}}",
                "POST _bulk" => """{"errors":false,"items":[]}""",
                "GET nhl-test/_alias" or "GET _alias/nhl-test" => """{"nhl-test-old":{"aliases":{"nhl-test":{}}}}""",
                _ when key.EndsWith("/_settings") && request.Method == HttpMethod.Get =>
                    $"{{\"{index}\":" + """{"settings":{"index":{"number_of_replicas":"1"}}}}""",
                _ when key.EndsWith("/_stats/docs") => Stats(index),
                _ => """{"acknowledged":true}""",
            };
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) };
        }

        private string Stats(string index)
        {
            var started = StartedCopies[Math.Min(_statsCalls++, StartedCopies.Length - 1)];
            var copies = string.Join(",", Enumerable.Range(0, 2).Select(i =>
                $$$"""{"routing":{"state":"{{{(i < started ? "STARTED" : "INITIALIZING")}}}"}}"""));
            return $"{{\"indices\":{{\"{index}\":{{\"shards\":{{\"0\":[{copies}]}}}}}}}}";
        }
    }
}
