using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Search;
using WebApi.BusinessLogic.SearchGetter;
using WebApi.Controllers;

namespace WebApi.Tests.BusinessLogic.UnitTests.SearchTests;

[TestClass]
public class SearchControllerTests
{
    private sealed class FakeSearch(Exception? fail = null) : ISearchGetter
    {
        public List<string> Queries { get; } = new();

        public Task<SearchResults> Search(string query, CancellationToken ct)
        {
            if (fail != null)
                throw fail;
            Queries.Add(query);
            return Task.FromResult(new SearchResults(query, [], [], [], []));
        }
    }

    private static SearchController Controller(ISearchGetter? search)
    {
        var services = new ServiceCollection();
        if (search != null)
            services.AddSingleton(search);
        return new SearchController(services.BuildServiceProvider(), NullLogger<SearchController>.Instance);
    }

    [TestMethod]
    public async Task Get_SearchOff_ReturnsNotFound()
    {
        var result = await Controller(null).Get("leafs", CancellationToken.None);

        result.Should().BeAssignableTo<IStatusCodeHttpResult>().Which.StatusCode.Should().Be(404);
    }

    [DataTestMethod]
    [DataRow(null)]
    [DataRow(" a ")]
    [DataRow("this query is far too long for anyone to type into a search box on a hockey site honestly, really it is")]
    public async Task Get_BadQuery_ReturnsBadRequest(string? q)
    {
        var search = new FakeSearch();

        var result = await Controller(search).Get(q, CancellationToken.None);

        result.Should().BeAssignableTo<IStatusCodeHttpResult>().Which.StatusCode.Should().Be(400);
        search.Queries.Should().BeEmpty();
    }

    [TestMethod]
    public async Task Get_TrimsTheQuery()
    {
        var search = new FakeSearch();

        var result = await Controller(search).Get("  leafs ", CancellationToken.None);

        result.Should().BeOfType<Ok<SearchResults>>();
        search.Queries.Should().Equal("leafs");
    }

    [TestMethod]
    public async Task Get_ElasticsearchFails_ReturnsServiceUnavailable()
    {
        var result = await Controller(new FakeSearch(new HttpRequestException("refused"))).Get("leafs", CancellationToken.None);

        result.Should().BeAssignableTo<IStatusCodeHttpResult>().Which.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
    }
}
