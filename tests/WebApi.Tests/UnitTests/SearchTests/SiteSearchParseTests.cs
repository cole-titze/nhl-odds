using FluentAssertions;
using Search;

namespace WebApi.Tests.BusinessLogic.UnitTests.SearchTests;

[TestClass]
public class SiteSearchParseTests
{
    private static readonly DateOnly Today = new(2026, 11, 3);

    [DataTestMethod]
    [DataRow("oilers march 3", "oilers", "2027-03-03")]   // next March is nearer than last March
    [DataRow("oilers oct 30", "oilers", "2026-10-30")]    // last week, not next October
    [DataRow("Oct. 30th leafs", "leafs", "2026-10-30")]
    [DataRow("sept 9 bruins", "bruins", "2026-09-09")]
    [DataRow("bruins 2026-01-10", "bruins", "2026-01-10")]
    [DataRow("edm 10/30", "edm", "2026-10-30")]
    [DataRow("edm 1/10/26", "edm", "2026-01-10")]
    [DataRow("oilers bruins oct 30, 2025", "oilers bruins", "2025-10-30")]
    [DataRow("tonight", "", "2026-11-03")]
    [DataRow("leafs yesterday", "leafs", "2026-11-02")]
    [DataRow("Tomorrow habs", "habs", "2026-11-04")]
    public void Parse_TakesTheDateOut(string query, string text, string date)
    {
        var parsed = SiteSearch.Parse(query, Today);

        parsed.Text.Should().Be(text);
        parsed.Date.Should().Be(DateOnly.Parse(date));
    }

    [DataTestMethod]
    [DataRow("connor mcdavid")]
    [DataRow("trade deadline")]
    [DataRow("may")]            // a month without a day
    [DataRow("feb 30 oilers")]  // no such day
    [DataRow("13/40")]
    [DataRow("top 10 goalies")]
    public void Parse_NoDate_KeepsTheText(string query)
    {
        var parsed = SiteSearch.Parse(query, Today);

        parsed.Date.Should().BeNull();
        parsed.Text.Should().Be(query);
    }

    [TestMethod]
    public void Parse_CollapsesWhitespace()
    {
        SiteSearch.Parse("  maple    leafs ", Today).Text.Should().Be("maple leafs");
    }

    [TestMethod]
    public void BuildRequest_SearchesEachIndexInOrder()
    {
        var lines = SiteSearch.BuildRequest(new ParsedQuery("leafs", null), new DateTime(2026, 11, 3, 12, 0, 0, DateTimeKind.Utc))
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);

        lines.Should().HaveCount(8);
        lines.Where((_, i) => i % 2 == 0).Should().Equal(
            SearchIndexes.All.Select(index => $"{{\"index\":\"{index.Alias}\"}}"));
    }

    [TestMethod]
    public void BuildRequest_DateOnly_SearchesGamesAlone()
    {
        var lines = SiteSearch.BuildRequest(new ParsedQuery("", new DateOnly(2026, 11, 3)), DateTime.UtcNow)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);

        lines[1].Should().Contain("match_none");  // teams
        lines[3].Should().Contain("match_none");  // players
        lines[5].Should().Contain("\"gameDate\":\"2026-11-03\"").And.NotContain("match_none");
        lines[7].Should().Contain("match_none");  // articles
    }

    [TestMethod]
    public void BuildSuggestRequest_SearchesTeamsPlayersAndGamesOnly_AFewOfEach()
    {
        var lines = SiteSearch.BuildSuggestRequest(new ParsedQuery("lea", null), new DateTime(2026, 11, 3, 12, 0, 0, DateTimeKind.Utc))
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);

        lines.Should().HaveCount(6);
        lines.Where((_, i) => i % 2 == 0).Should().Equal(
            new[] { SearchIndexes.Teams, SearchIndexes.Players, SearchIndexes.Games }.Select(index => $"{{\"index\":\"{index.Alias}\"}}"));
        lines[1].Should().Contain($"\"size\":{SiteSearch.SuggestTeams}").And.Contain("bool_prefix");
        lines[3].Should().Contain($"\"size\":{SiteSearch.SuggestPlayers}");
        lines[5].Should().Contain($"\"size\":{SiteSearch.SuggestGames}");
        string.Join('\n', lines).Should().NotContain("highlight").And.NotContain("knn");
    }

    [TestMethod]
    public void ReadSuggestions_ReadsTeamsPlayersAndGames()
    {
        var response = System.Text.Json.Nodes.JsonNode.Parse("""
            {"responses":[
              {"hits":{"hits":[{"_source":{"teamId":10,"name":"Toronto Maple Leafs","commonName":"Maple Leafs","placeName":"Toronto","formerNames":[],"abbreviation":"TOR","logoUri":"tor.svg","active":true,"lastSeason":2026}}]}},
              {"hits":{"hits":[{"_source":{"playerId":8479318,"fullName":"Auston Matthews","position":"C","teamId":10,"teamName":"Toronto Maple Leafs","teamAbbreviation":"TOR","isActive":true,"headshot":"am.png"}}]}},
              {"error":{"type":"index_not_found_exception"}}
            ]}
            """)!;

        var results = SiteSearch.ReadSuggestions("lea", response);

        results.Query.Should().Be("lea");
        results.Teams.Should().ContainSingle().Which.Name.Should().Be("Toronto Maple Leafs");
        results.Players.Should().ContainSingle().Which.PlayerId.Should().Be(8479318);
        results.Games.Should().BeEmpty();
    }
}
