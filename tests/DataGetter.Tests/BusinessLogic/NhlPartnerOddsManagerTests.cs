using DatabaseAccess.SnapshotRepository;
using DataGetter.BusinessLogic;
using Entities.DbModels;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Services.NhlData;

namespace DataGetter.Tests.BusinessLogic;

[TestClass]
public class NhlPartnerOddsManagerTests
{
    private static string Widget(string updated = "2026-10-10T02:00:38Z") => $$"""
        {
          "currentOddsDate": "2026-10-09",
          "lastUpdatedUTC": "{{updated}}",
          "bettingPartner": { "partnerId": 9, "country": "USA", "name": "DraftKings" },
          "games": [
            {
              "gameId": 2026020066,
              "startTimeUTC": "2026-10-09T23:00:00Z",
              "homeTeam": {
                "id": 17, "abbrev": "DET",
                "odds": [
                  { "description": "MONEY_LINE_2_WAY", "value": 135.0, "qualifier": "" },
                  { "description": "MONEY_LINE_3_WAY", "value": 180.0, "qualifier": "" },
                  { "description": "MONEY_LINE_3_WAY", "value": 350.0, "qualifier": "Draw" },
                  { "description": "PUCK_LINE", "value": -190.0, "qualifier": "+1.5" },
                  { "description": "OVER_UNDER", "value": -110.5, "qualifier": "O6.5" }
                ]
              },
              "awayTeam": {
                "id": 55, "abbrev": "SEA",
                "odds": [
                  { "description": "MONEY_LINE_2_WAY", "value": -160.0, "qualifier": "" },
                  { "description": "MONEY_LINE_3_WAY", "value": 350.0, "qualifier": "Draw" },
                  { "description": "MONEY_LINE_3_WAY", "value": 350.0, "qualifier": "Draw" },
                  { "description": "PUCK_LINE", "value": null, "qualifier": "-1.5" }
                ]
              }
            }
          ]
        }
        """;

    [TestMethod]
    public void ParseOdds_OneRowPerTeamPerLine()
    {
        var rows = NhlPartnerOddsManager.ParseOdds(Widget(), "US", new DateTime(2026, 10, 9, 20, 0, 0));

        // The away team's repeated Draw line is kept once and its unpriced puck line is skipped
        rows.Should().HaveCount(7);
        var puckLine = rows.Single(r => r.Market == "PUCK_LINE");
        puckLine.Should().BeEquivalentTo(new DbPartnerOdds
        {
            Country = "US",
            PartnerUpdatedUTC = new DateTime(2026, 10, 10, 2, 0, 38),
            GameId = 2026020066,
            TeamId = 17,
            Market = "PUCK_LINE",
            Qualifier = "+1.5",
            Line = 1.5m,
            Outcome = null,
            Price = -190m,
            IsHome = true,
            PartnerName = "DraftKings",
            OddsDate = new DateTime(2026, 10, 9),
            StartTimeUTC = new DateTime(2026, 10, 9, 23, 0, 0),
            FirstSeenUTC = new DateTime(2026, 10, 9, 20, 0, 0),
        });
        rows.Single(r => r.Market == "OVER_UNDER").Price.Should().Be(-110.5m);
        rows.Where(r => r.TeamId == 55).Should().OnlyContain(r => !r.IsHome);
    }

    [TestMethod]
    public void ParseQualifier_SplitsLineAndOutcome()
    {
        NhlPartnerOddsManager.ParseQualifier("").Should().Be(((decimal?)null, (string?)null));
        NhlPartnerOddsManager.ParseQualifier("-1.5").Should().Be(((decimal?)-1.5m, (string?)null));
        NhlPartnerOddsManager.ParseQualifier("O6.5").Should().Be(((decimal?)6.5m, "Over"));
        NhlPartnerOddsManager.ParseQualifier("U5.5").Should().Be(((decimal?)5.5m, "Under"));
        NhlPartnerOddsManager.ParseQualifier("Draw").Should().Be(((decimal?)null, "Draw"));
    }

    [TestMethod]
    public void ParseOdds_NoGamesIsEmpty()
    {
        var rows = NhlPartnerOddsManager.ParseOdds("""{ "lastUpdatedUTC": "2026-10-10T02:00:38Z", "games": [] }""", "CA", DateTime.UtcNow);

        rows.Should().BeEmpty();
    }

    [TestMethod]
    public async Task SavePartnerOdds_SavesOnlyNewVersions()
    {
        var repo = A.Fake<ISnapshotRepository>();
        var getter = A.Fake<INhlContentGetter>();
        A.CallTo(() => getter.GetPartnerOdds("US")).Returns(Widget());
        A.CallTo(() => repo.PartnerOddsVersionExists("US", new DateTime(2026, 10, 10, 2, 0, 38))).Returns(true);
        var manager = new NhlPartnerOddsManager(repo, getter, NullLoggerFactory.Instance);

        await manager.SavePartnerOdds("US", DateTime.UtcNow);
        A.CallTo(() => repo.AddPartnerOdds(A<IEnumerable<DbPartnerOdds>>._)).MustNotHaveHappened();

        A.CallTo(() => getter.GetPartnerOdds("US")).Returns(Widget("2026-10-10T03:00:00Z"));
        await manager.SavePartnerOdds("US", new DateTime(2026, 10, 10, 3, 5, 0, 500));
        A.CallTo(() => repo.AddPartnerOdds(A<IEnumerable<DbPartnerOdds>>.That.Matches(rows =>
            rows.Count() == 7 && rows.All(r => r.FirstSeenUTC == new DateTime(2026, 10, 10, 3, 5, 0))))).MustHaveHappenedOnceExactly();
    }
}