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
}
