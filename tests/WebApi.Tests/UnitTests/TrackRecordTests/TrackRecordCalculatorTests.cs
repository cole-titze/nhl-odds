using Entities.Models.Web;
using Entities.Types;
using FluentAssertions;
using WebApi.BusinessLogic.TrackRecord;

namespace WebApi.Tests.BusinessLogic.UnitTests.TrackRecordTests;

[TestClass]
public class TrackRecordCalculatorTests
{
    private const int YEAR = 2026;
    // 7 PM Central on Oct 10
    private static readonly DateTime Day1 = new(2026, 10, 11, 0, 0, 0, DateTimeKind.Utc);

    private static TrackRecordGame Game(double model, Winner winner, DateTime? date = null, bool live = true,
        TrackRecordPrice[]? morning = null, TrackRecordPrice[]? closing = null) => new()
    {
        GameId = Random.Shared.Next(),
        GameDateUTC = date ?? Day1,
        Winner = winner,
        ModelHomeOdds = model,
        IsLive = live,
        Morning = [.. morning ?? []],
        Closing = [.. closing ?? []],
    };

    private static TrackRecordPrice Price(string book, double home, double away) => new(book, home, away);

    [TestMethod]
    public void NoVig_ShouldRemoveTheBookmakersMargin()
    {
        TrackRecordCalculator.NoVig(Price("DK", 0.55, 0.50))!.Value.Should().BeApproximately(0.55 / 1.05, 1e-12);
        TrackRecordCalculator.NoVig(Price("DK", 0, 0.5)).Should().BeNull();
    }

    [TestMethod]
    public void LogLoss_ShouldScoreTheWinnersProbability()
    {
        TrackRecordCalculator.LogLoss(0.6, Winner.HOME).Should().BeApproximately(-Math.Log(0.6), 1e-12);
        TrackRecordCalculator.LogLoss(0.6, Winner.AWAY).Should().BeApproximately(-Math.Log(0.4), 1e-12);
        double.IsFinite(TrackRecordCalculator.LogLoss(1.0, Winner.AWAY)).Should().BeTrue();
    }

    [TestMethod]
    public void Calculate_ShouldSummarizeTheModel()
    {
        var result = TrackRecordCalculator.Calculate(YEAR, false,
            [Game(0.7, Winner.HOME), Game(0.6, Winner.AWAY), Game(0.4, Winner.AWAY, live: false)]);

        result.GameCount.Should().Be(3);
        result.LiveGameCount.Should().Be(2);
        result.ModelAccuracy.Should().BeApproximately(2.0 / 3, 1e-12);
        result.ModelLogLoss.Should().BeApproximately((-Math.Log(0.7) - Math.Log(0.4) - Math.Log(0.6)) / 3, 1e-12);
    }

    [TestMethod]
    public void Calculate_LiveOnly_ShouldLeaveOutBackfilledPredictions()
    {
        var result = TrackRecordCalculator.Calculate(YEAR, true,
            [Game(0.7, Winner.HOME), Game(0.4, Winner.AWAY, live: false)]);

        result.LiveOnly.Should().BeTrue();
        result.GameCount.Should().Be(1);
        result.ModelLogLoss.Should().BeApproximately(-Math.Log(0.7), 1e-12);
    }

    [TestMethod]
    public void Calculate_NoGames_ShouldReturnEmpty()
    {
        var result = TrackRecordCalculator.Calculate(YEAR, false, []);

        result.GameCount.Should().Be(0);
        result.ModelLogLoss.Should().BeNull();
        result.Calibration.Should().BeEmpty();
        result.LogLossByDate.Should().BeEmpty();
        result.Sources.Should().BeEmpty();
        result.ClosingLine.Should().BeNull();
    }

    [TestMethod]
    public void Calibration_ShouldGroupGamesIntoFivePercentBins()
    {
        var result = TrackRecordCalculator.Calculate(YEAR, false,
            [Game(0.61, Winner.HOME), Game(0.64, Winner.AWAY), Game(0.66, Winner.HOME), Game(1.0, Winner.HOME)]);

        result.Calibration.Select(b => (b.Low, b.High, b.Games)).Should().Equal(
            (0.6, 0.65, 2), (0.65, 0.7, 1), (0.95, 1.0, 1));
        result.Calibration[0].MeanPredicted.Should().BeApproximately(0.625, 1e-12);
        result.Calibration[0].HomeWinRate.Should().Be(0.5);
    }

    [TestMethod]
    public void LogLossByDate_ShouldRunPerCentralDayAndOnlyCountPricedGamesForTheMarket()
    {
        var result = TrackRecordCalculator.Calculate(YEAR, false,
        [
            Game(0.6, Winner.HOME, Day1),
            // 11:30 PM Central the same day, though it's the next day in UTC
            Game(0.5, Winner.HOME, Day1.AddHours(4.5), morning: [Price("DK", 0.55, 0.5)]),
            Game(0.8, Winner.AWAY, Day1.AddDays(1)),
        ]);

        result.LogLossByDate.Should().HaveCount(2);
        var first = result.LogLossByDate[0];
        first.Date.Should().Be(new DateTime(2026, 10, 10));
        first.Games.Should().Be(2);
        first.Model.Should().BeApproximately((-Math.Log(0.6) - Math.Log(0.5)) / 2, 1e-12);
        first.Market.Should().BeApproximately(-Math.Log(0.55 / 1.05), 1e-12);
        result.LogLossByDate[1].Games.Should().Be(3);
        result.LogLossByDate[1].Market.Should().Be(first.Market);
    }

    [TestMethod]
    public void Sources_ShouldScoreTheModelOnTheSameGamesAsEachBook()
    {
        var result = TrackRecordCalculator.Calculate(YEAR, false,
        [
            Game(0.7, Winner.HOME, morning: [Price("DK", 0.6, 0.45), Price("Kalshi", 0.62, 0.40)], closing: [Price("DK", 0.65, 0.4)]),
            Game(0.4, Winner.HOME, morning: [Price("Kalshi", 0.52, 0.5)]),
            Game(0.9, Winner.AWAY),
        ]);

        result.Sources.Select(s => (s.Name, s.Games)).Should().Equal(
            (TrackRecordCalculator.MorningAverage, 2),
            (TrackRecordCalculator.ClosingAverage, 1),
            ("Kalshi", 2),
            ("DK", 1));
        var dk = result.Sources.Single(s => s.Name == "DK");
        dk.LogLoss.Should().BeApproximately(-Math.Log(0.6 / 1.05), 1e-12);
        dk.ModelLogLoss.Should().BeApproximately(-Math.Log(0.7), 1e-12);
        var kalshi = result.Sources.Single(s => s.Name == "Kalshi");
        kalshi.ModelAccuracy.Should().Be(0.5);
        kalshi.Accuracy.Should().Be(1.0);
    }

    [TestMethod]
    public void ClosingLine_ShouldMeasureTheMoveTowardTheModel()
    {
        var result = TrackRecordCalculator.Calculate(YEAR, false,
        [
            // Opens 50%, model 60%, closes 55%: toward the model and the model is closer
            Game(0.6, Winner.HOME, morning: [Price("DK", 0.5, 0.5)], closing: [Price("DK", 0.55, 0.45)]),
            // Opens 50%, model 40%, closes 55%: away from the model
            Game(0.4, Winner.HOME, morning: [Price("DK", 0.5, 0.5)], closing: [Price("DK", 0.55, 0.45)]),
            // Closing book didn't price the morning: left out
            Game(0.6, Winner.HOME, morning: [Price("DK", 0.5, 0.5)], closing: [Price("FD", 0.55, 0.45)]),
        ]);

        var clv = result.ClosingLine!;
        clv.Games.Should().Be(2);
        clv.ModelCloser.Should().Be(1);
        clv.MovedTowardModel.Should().Be(1);
        clv.MovedAwayFromModel.Should().Be(1);
        clv.AverageMoveTowardModel.Should().BeApproximately(0, 1e-12);
    }
}
