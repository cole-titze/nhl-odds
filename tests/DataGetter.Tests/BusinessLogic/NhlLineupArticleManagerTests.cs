using DatabaseAccess.LineupArticleRepository;
using DataGetter.BusinessLogic;
using Entities.DbModels;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Services.NhlData;

namespace DataGetter.Tests.BusinessLogic;

[TestClass]
public class NhlLineupArticleManagerTests
{
    private static string Article(string text, string lastUpdated = "2026-10-10T04:26:36.892Z", string editor = "a") => $$"""
        {
          "lastUpdatedBy": "{{editor}}",
          "lastUpdatedDate": "{{lastUpdated}}",
          "contentDate": "2026-10-09T21:55:00Z",
          "parts": [
            { "type": "photo", "content": null },
            { "type": "markdown", "content": "{{text}}" },
            { "type": "external", "content": { "html": "<div></div>" } },
            { "type": "markdown", "content": "## **RANGERS at CAPITALS**" }
          ]
        }
        """;

    [TestMethod]
    public void ParseArticle_HashesMarkdownTextAndReadsDates()
    {
        var article = NhlLineupArticleManager.ParseArticle(Article("Projected lineups"), new DateTime(2026, 10, 10, 15, 0, 0));

        // sha256 of "Projected lineups\n## **RANGERS at CAPITALS**"
        article.ContentHash.Should().Be("de043379db7bc5e08acef78dfdc120457dbad565bc5a287ed913c5cdcc83678e");
        article.LastUpdated.Should().Be(new DateTime(2026, 10, 10, 4, 26, 36, 892));
        article.ContentDate.Should().Be(new DateTime(2026, 10, 9, 21, 55, 0));
        article.FirstSeenUTC.Should().Be(new DateTime(2026, 10, 10, 15, 0, 0));
    }

    [TestMethod]
    public void ParseArticle_MetadataOnlySaveHasSameHash()
    {
        var first = NhlLineupArticleManager.ParseArticle(Article("Lines"), DateTime.UtcNow);
        var resaved = NhlLineupArticleManager.ParseArticle(Article("Lines", "2026-10-10T05:00:00Z", "b"), DateTime.UtcNow);
        var edited = NhlLineupArticleManager.ParseArticle(Article("Lines edited"), DateTime.UtcNow);

        resaved.ContentHash.Should().Be(first.ContentHash);
        edited.ContentHash.Should().NotBe(first.ContentHash);
    }

    [TestMethod]
    public async Task SaveLineupArticle_SavesOnlyNewText()
    {
        var repo = A.Fake<ILineupArticleRepository>();
        var getter = A.Fake<INhlLineupArticleGetter>();
        A.CallTo(() => getter.GetLineupArticle()).Returns(Article("Lines"));
        var known = NhlLineupArticleManager.ParseArticle(Article("Lines"), DateTime.UtcNow).ContentHash;
        A.CallTo(() => repo.Exists(known)).Returns(true);
        var manager = new NhlLineupArticleManager(repo, getter, NullLoggerFactory.Instance);

        await manager.SaveLineupArticle(DateTime.UtcNow);
        A.CallTo(() => repo.Add(A<DbLineupArticle>._)).MustNotHaveHappened();

        A.CallTo(() => getter.GetLineupArticle()).Returns(Article("New lines"));
        await manager.SaveLineupArticle(new DateTime(2026, 10, 10, 16, 30, 5, 250));
        A.CallTo(() => repo.Add(A<DbLineupArticle>.That.Matches(a =>
            a.ContentHash != known && a.FirstSeenUTC == new DateTime(2026, 10, 10, 16, 30, 5)))).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task SaveLineupArticle_ThrowsWhenFetchFails()
    {
        var getter = A.Fake<INhlLineupArticleGetter>();
        A.CallTo(() => getter.GetLineupArticle()).Returns((string?)null);
        var manager = new NhlLineupArticleManager(A.Fake<ILineupArticleRepository>(), getter, NullLoggerFactory.Instance);

        var act = () => manager.SaveLineupArticle(DateTime.UtcNow);

        await act.Should().ThrowAsync<Exception>();
    }
}