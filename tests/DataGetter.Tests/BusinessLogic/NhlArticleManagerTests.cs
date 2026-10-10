using DatabaseAccess.SnapshotRepository;
using DataGetter.BusinessLogic;
using Entities.DbModels;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Services.NhlData;

namespace DataGetter.Tests.BusinessLogic;

[TestClass]
public class NhlArticleManagerTests
{
    private const string URL = "https://forge-dapi.d3.nhle.com/v2/content/en-us/stories/game-preview-10-10-26";

    private static string Story(string text, string id = "e1", string lastUpdated = "2026-10-10T11:00:02.097Z") => $$"""
        {
          "_entityId": "{{id}}",
          "slug": "game-preview-10-10-26",
          "title": "Game preview",
          "headline": "Avalanche host Kings",
          "summary": "Preview",
          "contentDate": "2026-10-10T11:00:00Z",
          "lastUpdatedDate": "{{lastUpdated}}",
          "tags": [
            { "slug": "2026-27", "title": "2026-27", "externalSourceName": "taxonomy" },
            { "slug": "teamid-21", "title": "Colorado Avalanche", "externalSourceName": "team" },
            { "slug": "playerid-8477492", "title": "Nathan MacKinnon", "externalSourceName": "player" },
            { "slug": "gameid-2026020070", "title": "2026020070", "externalSourceName": "game" },
            { "slug": "teamid-21", "title": "Colorado Avalanche", "externalSourceName": "team" }
          ],
          "parts": [
            { "type": "photo", "content": null },
            { "type": "markdown", "content": "{{text}}" },
            { "type": "customentity", "content": { "id": 1 } },
            { "type": "markdown", "content": "**Projected lineups**" }
          ]
        }
        """;

    private static string Feed(string lastUpdated = "2026-10-10T11:00:02.097Z") => $$"""
        { "items": [ { "_entityId": "e1", "selfUrl": "{{URL}}", "lastUpdatedDate": "{{lastUpdated}}" } ] }
        """;

    [TestMethod]
    public void ParseArticle_ReadsTextAndTags()
    {
        var (article, tags) = NhlArticleManager.ParseArticle(Story("Avalanche at home"), new DateTime(2026, 10, 10, 15, 0, 0));

        article.EntityId.Should().Be("e1");
        article.Slug.Should().Be("game-preview-10-10-26");
        article.Headline.Should().Be("Avalanche host Kings");
        article.Body.Should().Be("Avalanche at home\n**Projected lineups**");
        article.LastUpdated.Should().Be(new DateTime(2026, 10, 10, 11, 0, 2, 97));
        article.ContentHash.Should().HaveLength(64);

        tags.Should().HaveCount(4);
        tags.Single(t => t.TagSlug == "teamid-21").Should().Match<DbNhlArticleTag>(t => t.IdType == "team" && t.NhlId == 21);
        tags.Single(t => t.TagSlug == "playerid-8477492").NhlId.Should().Be(8477492);
        tags.Single(t => t.TagSlug == "gameid-2026020070").IdType.Should().Be("game");
        tags.Single(t => t.TagSlug == "2026-27").NhlId.Should().BeNull();
    }

    [TestMethod]
    public async Task SaveLatestArticles_SkipsArticlesAlreadySavedAtTheirLastUpdate()
    {
        var repo = A.Fake<ISnapshotRepository>();
        var getter = A.Fake<INhlContentGetter>();
        A.CallTo(() => getter.GetLatestStories(A<int>._)).Returns(Feed());
        A.CallTo(() => repo.GetArticleVersions(A<IEnumerable<string>>._)).Returns(
            [new DbNhlArticle { EntityId = "e1", ContentHash = "h", LastUpdated = new DateTime(2026, 10, 10, 11, 0, 2, 97) }]);
        var manager = new NhlArticleManager(repo, getter, NullLoggerFactory.Instance);

        var failures = await manager.SaveLatestArticles(DateTime.UtcNow);

        failures.Should().BeEmpty();
        A.CallTo(() => getter.GetStoryByUrl(A<string>._)).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task SaveLatestArticles_SavesNewTextAndOnlyNewTags()
    {
        var repo = A.Fake<ISnapshotRepository>();
        var getter = A.Fake<INhlContentGetter>();
        A.CallTo(() => getter.GetLatestStories(A<int>._)).Returns(Feed("2026-10-10T12:00:00Z"));
        A.CallTo(() => getter.GetStoryByUrl(URL)).Returns(Story("Edited", lastUpdated: "2026-10-10T12:00:00Z"));
        A.CallTo(() => repo.GetArticleVersions(A<IEnumerable<string>>._)).Returns(
            [new DbNhlArticle { EntityId = "e1", ContentHash = "old", LastUpdated = new DateTime(2026, 10, 10, 11, 0, 2, 97) }]);
        A.CallTo(() => repo.GetArticleTags("e1")).Returns([new DbNhlArticleTag { EntityId = "e1", TagSlug = "2026-27" }]);
        var manager = new NhlArticleManager(repo, getter, NullLoggerFactory.Instance);

        await manager.SaveLatestArticles(DateTime.UtcNow);

        A.CallTo(() => repo.AddArticle(A<DbNhlArticle>.That.Matches(a => a.Body.StartsWith("Edited")))).MustHaveHappenedOnceExactly();
        A.CallTo(() => repo.AddArticleTags(A<IEnumerable<DbNhlArticleTag>>.That.Matches(t =>
            t.Count() == 3 && t.All(x => x.TagSlug != "2026-27")))).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task SaveLatestArticles_MetadataOnlySaveMovesLastUpdated()
    {
        var repo = A.Fake<ISnapshotRepository>();
        var getter = A.Fake<INhlContentGetter>();
        var (current, _) = NhlArticleManager.ParseArticle(Story("Same"), DateTime.UtcNow);
        A.CallTo(() => getter.GetLatestStories(A<int>._)).Returns(Feed("2026-10-10T12:00:00Z"));
        A.CallTo(() => getter.GetStoryByUrl(URL)).Returns(Story("Same", lastUpdated: "2026-10-10T12:00:00Z"));
        A.CallTo(() => repo.GetArticleVersions(A<IEnumerable<string>>._)).Returns(
            [new DbNhlArticle { EntityId = "e1", ContentHash = current.ContentHash, LastUpdated = current.LastUpdated }]);
        var manager = new NhlArticleManager(repo, getter, NullLoggerFactory.Instance);

        await manager.SaveLatestArticles(DateTime.UtcNow);

        A.CallTo(() => repo.AddArticle(A<DbNhlArticle>._)).MustNotHaveHappened();
        A.CallTo(() => repo.SetArticleLastUpdated("e1", current.ContentHash, new DateTime(2026, 10, 10, 12, 0, 0))).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task SaveLatestArticles_ReturnsFailuresAndKeepsGoing()
    {
        var repo = A.Fake<ISnapshotRepository>();
        var getter = A.Fake<INhlContentGetter>();
        A.CallTo(() => getter.GetLatestStories(A<int>._)).Returns($$"""
            { "items": [
              { "_entityId": "bad", "selfUrl": "{{URL}}-bad", "lastUpdatedDate": "2026-10-10T12:00:00Z" },
              { "_entityId": "e1", "selfUrl": "{{URL}}", "lastUpdatedDate": "2026-10-10T12:00:00Z" } ] }
            """);
        A.CallTo(() => getter.GetStoryByUrl(URL + "-bad")).ThrowsAsync(new HttpRequestException("503"));
        A.CallTo(() => getter.GetStoryByUrl(URL)).Returns(Story("Text"));
        A.CallTo(() => repo.GetArticleVersions(A<IEnumerable<string>>._)).Returns([]);
        var manager = new NhlArticleManager(repo, getter, NullLoggerFactory.Instance);

        var failures = await manager.SaveLatestArticles(DateTime.UtcNow);

        failures.Should().ContainSingle().Which.Url.Should().Be(URL + "-bad");
        A.CallTo(() => repo.AddArticle(A<DbNhlArticle>.That.Matches(a => a.EntityId == "e1"))).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task SaveLatestArticles_RejectsAStoryThatIsADifferentArticle()
    {
        var repo = A.Fake<ISnapshotRepository>();
        var getter = A.Fake<INhlContentGetter>();
        A.CallTo(() => getter.GetLatestStories(A<int>._)).Returns(Feed("2026-10-10T12:00:00Z"));
        A.CallTo(() => getter.GetStoryByUrl(URL)).Returns(Story("Other team's preview", id: "e2"));
        A.CallTo(() => repo.GetArticleVersions(A<IEnumerable<string>>._)).Returns([]);
        var manager = new NhlArticleManager(repo, getter, NullLoggerFactory.Instance);

        var failures = await manager.SaveLatestArticles(DateTime.UtcNow);

        failures.Should().ContainSingle();
        A.CallTo(() => repo.AddArticle(A<DbNhlArticle>._)).MustNotHaveHappened();
    }
}