using System.Text.Json.Nodes;
using Entities.DbModels;
using FluentAssertions;
using Search;

namespace WebApi.Tests.BusinessLogic.UnitTests.SearchTests;

[TestClass]
public class SearchDocumentMapperTests
{
    private static DbSeasonTeam Team(int id, int season, string place, string common, string abbr) => new()
    {
        TeamId = id, SeasonStartYear = season, PlaceName = place, CommonName = common, Name = $"{place} {common}", Abbreviation = abbr,
    };

    [TestMethod]
    public void MapTeams_UsesTheLatestNameAndKeepsFormerOnes()
    {
        var teams = SearchDocumentMapper.MapTeams([
            Team(59, 2024, "Utah", "Hockey Club", "UTA"),
            Team(59, 2025, "Utah", "Mammoth", "UTA"),
            Team(53, 2023, "Arizona", "Coyotes", "ARI"),
        ]);

        teams.Should().HaveCount(2);
        var utah = teams.Single(t => t.TeamId == 59);
        utah.Name.Should().Be("Utah Mammoth");
        utah.FormerNames.Should().Equal("Utah Hockey Club");
        utah.Active.Should().BeTrue();
        teams.Single(t => t.TeamId == 53).Active.Should().BeFalse();
    }

    [TestMethod]
    public void MapGames_NamesTeamsAsThatSeasonAndUsesTheEasternDate()
    {
        var seasonTeams = new List<DbSeasonTeam>
        {
            Team(59, 2024, "Utah", "Hockey Club", "UTA"), Team(59, 2025, "Utah", "Mammoth", "UTA"),
            Team(6, 2024, "Boston", "Bruins", "BOS"),
        };
        var games = SearchDocumentMapper.MapGames([
            // 01:30 UTC on Jan 11 is 8:30 PM on Jan 10 in Eastern time
            new DbGameRaw { Id = 2024020500, HomeTeamId = 59, AwayTeamId = 6, SeasonStartYear = 2024,
                GameDateUTC = new DateTime(2025, 1, 11, 1, 30, 0), HomeGoals = 3, AwayGoals = 2, HasBeenPlayed = true },
            new DbGameRaw { Id = 2024020501, HomeTeamId = 6, AwayTeamId = 99, SeasonStartYear = 2024, GameDateUTC = DateTime.UtcNow },
        ], seasonTeams);

        games.Should().ContainSingle();  // team 99 is unknown
        games[0].GameDate.Should().Be("2025-01-10");
        games[0].Home.Name.Should().Be("Utah Hockey Club");
        games[0].Home.Goals.Should().Be(3);
        games[0].Away.Name.Should().Be("Boston Bruins");
    }

    [TestMethod]
    public void MapGames_Unplayed_HasNoGoals()
    {
        var games = SearchDocumentMapper.MapGames(
            [new DbGameRaw { Id = 1, HomeTeamId = 6, AwayTeamId = 6, SeasonStartYear = 2024, GameDateUTC = DateTime.UtcNow }],
            [Team(6, 2024, "Boston", "Bruins", "BOS")]);

        games[0].Home.Goals.Should().BeNull();
        games[0].Played.Should().BeFalse();
    }

    [TestMethod]
    public void MapPlayers_UsesTheCurrentTeam_LeavesNoneOrUnknownEmpty()
    {
        var teams = SearchDocumentMapper.MapTeams([Team(22, 2026, "Edmonton", "Oilers", "EDM")]);
        var players = SearchDocumentMapper.MapPlayers([
            // Player.CurrentTeamId is stale (set on first fetch); the row's current team wins
            new PlayerRow(new DbPlayer { Id = 1, FirstName = "Connor", LastName = "McDavid", CurrentTeamId = 77, IsActive = true }, "C", 22),
            new PlayerRow(new DbPlayer { Id = 2, FirstName = "Free", LastName = "Agent", CurrentTeamId = 22 }, null, null),
            new PlayerRow(new DbPlayer { Id = 3, FirstName = "Gone", LastName = "Team" }, "D", 77),
        ], teams);

        players[0].Should().Be(new PlayerDocument(1, "Connor McDavid", "C", 22, "Edmonton Oilers", "EDM", true, ""));
        players[1].TeamId.Should().BeNull();
        players[1].Position.Should().BeNull();
        players[2].TeamId.Should().BeNull();
    }

    [TestMethod]
    public void MapArticles_KeepsTheNewestVersionAsPlainText()
    {
        var articles = SearchDocumentMapper.MapArticles([
            new DbNhlArticle { EntityId = "a", Slug = "trade-tracker", Title = "Old", Body = "old", FirstSeenUTC = new DateTime(2026, 10, 1) },
            new DbNhlArticle { EntityId = "a", Slug = "trade-tracker", Title = "Tracker", Headline = "Trade Tracker", Body = "**New** [deal](https://x)",
                FirstSeenUTC = new DateTime(2026, 11, 1) },
        ]);

        articles.Should().ContainSingle();
        articles[0].Title.Should().Be("Trade Tracker");
        articles[0].Body.Should().Be("New deal");
        articles[0].Url.Should().Be("https://www.nhl.com/news/trade-tracker");
    }

    [TestMethod]
    public void MapArticles_ATeamSiteRepostCountsOnce()
    {
        static DbNhlArticle Article(string id, string slug, int day) => new()
        {
            EntityId = id, Slug = slug, Title = "Sharks at Senators recap", Body = id,
            ContentDate = new DateTime(2026, 10, day), FirstSeenUTC = new DateTime(2026, 10, day),
        };
        var articles = SearchDocumentMapper.MapArticles([
            // The league's original and two team-site reposts: the original is kept, though not the newest
            Article("league", "sharks-senators-recap-october-22-2026", 22),
            Article("team1", "sharks-senators-recap-october-22-2026-x7708", 23),
            Article("team2", "sharks-senators-recap-october-22-2026-x0939", 24),
            // Reposts whose original isn't stored: the newest
            Article("old", "edge-stats-mcdavid-x2862", 2),
            Article("new", "edge-stats-mcdavid-x1234", 7),
            // Same title, another day: a different article
            Article("other", "sharks-senators-recap-october-31-2026", 31),
            // Not a repost suffix (5 digits, or no x)
            Article("five", "story-x12345", 1),
            Article("year", "season-preview-2026", 1),
        ]);

        articles.Select(a => a.EntityId).Should().BeEquivalentTo("league", "new", "other", "five", "year");
    }

    [DataTestMethod]
    [DataRow("## Oilers 5, Flames 2\n\n**Connor McDavid** scored", "Oilers 5, Flames 2 Connor McDavid scored")]
    [DataRow("A [hat trick](https://nhl.com/x) for *Leon*", "A hat trick for Leon")]
    [DataRow("![photo](https://img) - first\n- second", "first second")]
    [DataRow("St. Louis and 5*3 = 15", "St. Louis and 5*3 = 15")]
    [DataRow("EDMONTON -- <forge-entity title=\"Connor McDavid\" slug=\"connor-mcdavid-8478402\" code=\"player\">Connor McDavid</forge-entity> is off", "EDMONTON -- Connor McDavid is off")]
    [DataRow("his 1,000<sup>th</sup> point, <a href=\"https://nhl.com\">recap</a> &amp; <s>old</s> new", "his 1,000th point, recap & old new")]
    [DataRow("a 3 < 4 lead, then 5 > 2", "a 3 < 4 lead, then 5 > 2")]
    public void MarkdownToText_RemovesFormatting(string markdown, string text)
    {
        SearchDocumentMapper.MarkdownToText(markdown).Should().Be(text);
    }

    [TestMethod]
    public void ReadResponse_AFailedIndexGivesNoResults()
    {
        var response = JsonNode.Parse("""
            {"responses":[
              {"hits":{"hits":[{"_source":{"teamId":10,"name":"Toronto Maple Leafs","commonName":"Maple Leafs","placeName":"Toronto","formerNames":[],"abbreviation":"TOR","logoUri":"x","active":true,"lastSeason":2026}}]}},
              {"error":{"type":"index_not_found_exception"},"status":404},
              {"hits":{"hits":[]}},
              {"hits":{"hits":[{"_source":{"title":"T","url":"u","date":"2026-11-01T00:00:00Z"},"highlight":{"body":["a leafs b"]}}]}}
            ]}
            """)!;

        var results = SiteSearch.ReadResponse("leafs", response);

        results.Teams.Should().ContainSingle().Which.Abbreviation.Should().Be("TOR");
        results.Players.Should().BeEmpty();
        results.Articles.Should().ContainSingle().Which.Snippet.Should().Be("a leafs b");
    }
}
