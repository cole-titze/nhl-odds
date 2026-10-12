using Entities.DbModels;
using Entities.ServiceModels.Mappers;
using FluentAssertions;

namespace BookmakerOddsGetter.Tests;

[TestClass]
public class OddsApiClosingOddsFetcherTests
{
    // 2026-01-15 12:00 UTC = 06:00 CST
    private static readonly DateTime Jan15Morning = new(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime FebReset = new(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public void ChooseMarkets_EnoughForEveryLaterStart_FetchesAllThree()
    {
        // 10 starts left (this one + 9) at 3 credits = 30, plus 6 held back
        OddsApiClosingOddsFetcher.ChooseMarkets(36, 6, 9).Should().Be("h2h,spreads,totals");
    }

    [TestMethod]
    public void ChooseMarkets_ShortForThree_DropsSpread()
    {
        OddsApiClosingOddsFetcher.ChooseMarkets(35, 6, 9).Should().Be("h2h,totals");
    }

    [TestMethod]
    public void ChooseMarkets_ShortForTwo_MoneylineOnly()
    {
        OddsApiClosingOddsFetcher.ChooseMarkets(25, 6, 9).Should().Be("h2h");
    }

    [TestMethod]
    public void ChooseMarkets_ShortEvenForMoneyline_StillSpendsWhatIsLeft()
    {
        OddsApiClosingOddsFetcher.ChooseMarkets(8, 6, 9).Should().Be("h2h");
    }

    [TestMethod]
    public void ChooseMarkets_OnlyTheReserveLeft_ReturnsNull()
    {
        OddsApiClosingOddsFetcher.ChooseMarkets(6, 6, 0).Should().BeNull();
    }

    [TestMethod]
    public void MorningCallsLeft_CountsRunsBeforeResetWithGamesAfterThem()
    {
        // Now is just after today's run; runs Jan 16..31 (16) all precede the last game
        var now = Jan15Morning.AddMinutes(1);
        var starts = new[] { new DateTime(2026, 2, 3, 0, 0, 0, DateTimeKind.Utc) };

        OddsApiClosingOddsFetcher.MorningCallsLeft(now, FebReset, starts).Should().Be(16);
    }

    [TestMethod]
    public void MorningCallsLeft_StopsAtTheLastGame()
    {
        var now = Jan15Morning.AddMinutes(1);
        var starts = new[] { new DateTime(2026, 1, 18, 0, 0, 0, DateTimeKind.Utc) };

        // Runs Jan 16 and 17; Jan 18's run is after the last game
        OddsApiClosingOddsFetcher.MorningCallsLeft(now, FebReset, starts).Should().Be(2);
    }

    [TestMethod]
    public void MorningCallsLeft_NoGames_ReturnsZero()
    {
        OddsApiClosingOddsFetcher.MorningCallsLeft(Jan15Morning, FebReset, []).Should().Be(0);
    }

    [TestMethod]
    public void ToClosing_CopiesPricesAndStampsPriceTime()
    {
        var priceTime = new DateTime(2026, 1, 15, 23, 59, 0, DateTimeKind.Utc);
        var mapped = new OddsApiResponseMapper.MappedOdds
        {
            H2H =
            [
                new DbBookmakerOdds { GameId = 1, BookmakerName = "DraftKings", BookmakerKey = "draftkings", HomeOdds = 0.6, AwayOdds = 0.44, OddsApiGameId = "abc" },
                new DbBookmakerOdds { GameId = 1, BookmakerName = "DraftKings", BookmakerKey = "draftkings", HomeOdds = 0.5, AwayOdds = 0.5, OddsApiGameId = "abc" },
            ],
            Spreads = [new DbBookmakerSpreads { GameId = 1, BookmakerName = "DraftKings", HomePoint = -1.5, HomePrice = 170, AwayPoint = 1.5, AwayPrice = -200 }],
            Totals = [new DbBookmakerTotals { GameId = 1, BookmakerName = "DraftKings", OverUnderPoint = 6.5, OverPrice = -110, UnderPrice = -110 }],
        };

        var (h2h, spreads, totals) = OddsApiClosingOddsFetcher.ToClosing(mapped, priceTime, priceTime);

        h2h.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            GameId = 1, BookmakerKey = "draftkings", HomeOdds = 0.6, AwayOdds = 0.44, PriceTimeUTC = priceTime, OddsApiGameId = "abc",
        });
        spreads.Should().ContainSingle().Which.Should().BeEquivalentTo(new { HomePoint = -1.5, HomePrice = 170, AwayPrice = -200, PriceTimeUTC = priceTime });
        totals.Should().ContainSingle().Which.Should().BeEquivalentTo(new { OverUnderPoint = 6.5, OverPrice = -110, UnderPrice = -110 });
    }

    [TestMethod]
    public void ToClosing_UsesTheMarketLastUpdateWhenGiven()
    {
        var lastUpdate = new DateTime(2026, 1, 15, 23, 40, 0, DateTimeKind.Utc);
        var mapped = new OddsApiResponseMapper.MappedOdds
        {
            H2H = [new DbBookmakerOdds { GameId = 1, BookmakerName = "FanDuel", MarketLastUpdate = lastUpdate }],
        };

        var (h2h, _, _) = OddsApiClosingOddsFetcher.ToClosing(mapped, lastUpdate.AddMinutes(19), lastUpdate.AddMinutes(19));

        h2h.Single().PriceTimeUTC.Should().Be(lastUpdate);
    }
}
