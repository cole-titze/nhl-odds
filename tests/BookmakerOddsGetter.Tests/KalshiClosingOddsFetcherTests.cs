using System.Text.Json;
using Entities.ServiceModels.Kalshi;
using FluentAssertions;

namespace BookmakerOddsGetter.Tests;

[TestClass]
public class KalshiClosingOddsFetcherTests
{
    private const long Start = 1_791_586_800;

    private static KalshiCandlestick Candle(long endTs, string ask) =>
        new() { EndPeriodTs = endTs, YesAsk = new KalshiOhlc { CloseDollars = ask } };

    [TestMethod]
    public void LastQuoteBefore_TakesLastCandleEndingByTheStart()
    {
        var candles = new[] { Candle(Start - 120, "0.48"), Candle(Start, "0.49"), Candle(Start + 60, "0.70") };

        KalshiClosingOddsFetcher.LastQuoteBefore(candles, Start)!.YesAsk.Close.Should().Be(0.49);
    }

    [TestMethod]
    public void LastQuoteBefore_SkipsCandlesWithoutAnAsk()
    {
        var candles = new[] { Candle(Start - 120, "0.48"), Candle(Start - 60, "0") };

        KalshiClosingOddsFetcher.LastQuoteBefore(candles, Start)!.EndPeriodTs.Should().Be(Start - 120);
    }

    [TestMethod]
    public void LastQuoteBefore_NoCandleBeforeStart_ReturnsNull()
    {
        KalshiClosingOddsFetcher.LastQuoteBefore([Candle(Start + 60, "0.50")], Start).Should().BeNull();
    }

    [TestMethod]
    public void Close_ReadsHistoricalEndpointShape()
    {
        const string json = """{"end_period_ts":1,"yes_ask":{"close":"0.7200"},"yes_bid":{"close":null}}""";

        var candle = JsonSerializer.Deserialize<KalshiCandlestick>(json)!;

        candle.YesAsk.Close.Should().Be(0.72);
        candle.YesBid.Close.Should().Be(0);
    }
}