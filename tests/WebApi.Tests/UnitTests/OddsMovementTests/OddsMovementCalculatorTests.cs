using Entities.Models.Web;
using FluentAssertions;
using WebApi.BusinessLogic.OddsMovement;

namespace WebApi.Tests.BusinessLogic.UnitTests.OddsMovementTests;

[TestClass]
public class OddsMovementCalculatorTests
{
    private static readonly DateTime Start = new(2026, 11, 4, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Morning = Start.AddHours(-12);

    private static OddsMovementGame Game() => new() { GameId = 2026020150, StartUTC = Start };

    [TestMethod]
    public void Calculate_NoPrices_ShouldReturnNoSeries()
    {
        var result = OddsMovementCalculator.Calculate(Game());

        result.Series.Should().BeEmpty();
        result.Move.Should().BeNull();
    }

    [TestMethod]
    public void Calculate_ShouldStartAtMidnightEasternOnGameDay_CarryingInTheLastEarlierValue()
    {
        // Start is 7 PM ET on Nov 3 (EST), so game day starts at 05:00 UTC Nov 3
        var dayStart = new DateTime(2026, 11, 3, 5, 0, 0, DateTimeKind.Utc);
        OddsMovementCalculator.GameDayStartUtc(Start).Should().Be(dayStart);
        var game = Game();
        game.ModelRuns =
        [
            new ModelRun(Start.AddDays(-20), 0.40),
            new ModelRun(Start.AddDays(-2), 0.52),
            new ModelRun(Morning, 0.55),
        ];

        var model = OddsMovementCalculator.Calculate(game).Series.Single().Points;

        model.Select(p => p.TimeUTC).Should().Equal(dayStart, Morning);
        model.Select(p => p.HomeProbability).Should().Equal(0.52, 0.55);
    }

    [TestMethod]
    public void Calculate_OnlyEarlierRuns_ShouldKeepOnePointAtGameDayStart()
    {
        var game = Game();
        game.ModelRuns = [new ModelRun(Start.AddDays(-3), 0.48), new ModelRun(Start.AddDays(-1), 0.5)];

        var model = OddsMovementCalculator.Calculate(game).Series.Single().Points;

        model.Should().ContainSingle().Which.HomeProbability.Should().Be(0.5);
        model[0].TimeUTC.Should().Be(OddsMovementCalculator.GameDayStartUtc(Start));
    }

    [TestMethod]
    public void Calculate_ShouldLeaveOutAnythingAfterPuckDrop()
    {
        var game = Game();
        game.ModelRuns = [new ModelRun(Morning, 0.55), new ModelRun(Start.AddHours(2), 0.9)];

        var model = OddsMovementCalculator.Calculate(game).Series.Single();

        model.Name.Should().Be(OddsMovementCalculator.Model);
        model.Points.Select(p => p.HomeProbability).Should().Equal(0.55);
    }

    [TestMethod]
    public void Calculate_PartnerVersions_ShouldRemoveTheMarginAndKeepOnlyChanges()
    {
        var game = Game();
        game.Partner =
        [
            // -150/+130: 0.6 and 0.4348 implied
            new PartnerPrice("DraftKings (US)", Morning.AddHours(2), -150, 130),
            // Same moneyline, a later version (another market changed)
            new PartnerPrice("DraftKings (US)", Morning.AddHours(3), -150, 130),
            new PartnerPrice("DraftKings (US)", Morning.AddHours(1), -140, 120),
            new PartnerPrice("FanDuel (CA)", Morning, -110, -110),
        ];

        var series = OddsMovementCalculator.Calculate(game).Series;

        series.Select(s => s.Name).Should().Equal("DraftKings (US)", "FanDuel (CA)");
        var dk = series[0].Points;
        dk.Select(p => p.TimeUTC).Should().Equal(Morning.AddHours(1), Morning.AddHours(2));
        dk[1].HomeProbability.Should().BeApproximately(0.6 / (0.6 + 100.0 / 230), 1e-9);
        series[1].Points.Single().HomeProbability.Should().BeApproximately(0.5, 1e-9);
    }

    [TestMethod]
    public void Calculate_ShouldAverageSportsbooksAndKeepKalshiApart()
    {
        var game = Game();
        game.Morning =
        [
            new BookPrice("DraftKings", Morning, 0.55, 0.50),
            new BookPrice("FanDuel", Morning.AddMinutes(1), 0.50, 0.50),
            new BookPrice("Kalshi", Morning, 0.60, 0.42),
        ];
        game.Closing =
        [
            new BookPrice("DraftKings", Start.AddMinutes(-1), 0.60, 0.45),
            new BookPrice("Kalshi", Start.AddMinutes(-1), 0.62, 0.40),
        ];

        var series = OddsMovementCalculator.Calculate(game).Series;

        var average = series.Single(s => s.Name == OddsMovementCalculator.SportsbookAverage).Points;
        average.Select(p => p.TimeUTC).Should().Equal(Morning.AddMinutes(1), Start.AddMinutes(-1));
        average[0].HomeProbability.Should().BeApproximately((0.55 / 1.05 + 0.5) / 2, 1e-9);
        average[1].HomeProbability.Should().BeApproximately(0.60 / 1.05, 1e-9);
        var kalshi = series.Single(s => s.Name == OddsMovementCalculator.Kalshi).Points;
        kalshi.Select(p => p.HomeProbability).Should().Equal(0.60 / 1.02, 0.62 / 1.02);
    }

    [TestMethod]
    public void Move_ShouldUseBooksWithBothPricesAndSignItTowardTheModel()
    {
        var game = Game();
        game.ModelRuns = [new ModelRun(Morning, 0.40), new ModelRun(Morning.AddHours(1), 0.70)];
        game.Morning = [new BookPrice("DraftKings", Morning, 0.5, 0.5), new BookPrice("FanDuel", Morning, 0.3, 0.7)];
        // FanDuel has no close, so only DraftKings counts: 50% to 60%
        game.Closing = [new BookPrice("DraftKings", Start, 0.6, 0.4)];

        var move = OddsMovementCalculator.Calculate(game).Move!;

        move.Open.Should().BeApproximately(0.5, 1e-9);
        move.Close.Should().BeApproximately(0.6, 1e-9);
        // The last model run before puck drop (70%) is above the open, so a rise is toward it
        move.Model.Should().Be(0.70);
        move.TowardModel.Should().BeApproximately(0.1, 1e-9);
    }

    [TestMethod]
    public void Move_WithoutAModel_ShouldStillGiveOpenAndClose()
    {
        var game = Game();
        game.Morning = [new BookPrice("DraftKings", Morning, 0.5, 0.5)];
        game.Closing = [new BookPrice("DraftKings", Start, 0.4, 0.6)];

        var move = OddsMovementCalculator.Calculate(game).Move!;

        move.Close.Should().BeApproximately(0.4, 1e-9);
        move.Model.Should().BeNull();
        move.TowardModel.Should().BeNull();
    }
}
