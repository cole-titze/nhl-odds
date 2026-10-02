using Entities.Types;
using Entities.ViewModels;
using FluentAssertions;
using WebApi.BusinessLogic.StrategyBacktester;

namespace WebApi.Tests.BusinessLogic.UnitTests.StrategyBacktesterTests;

[TestClass]
public class StrategyRulesTests
{
    private static GameOddsVM Game(
        double homeModel, int homeGoals, int awayGoals, params BookmakerOddsVM[] books) => new()
    {
        HasBeenPlayed = true,
        Winner = homeGoals > awayGoals ? Winner.HOME : Winner.AWAY,
        HomeTeam = new MatchupTeamVM { ModelOdds = homeModel, Goals = homeGoals },
        AwayTeam = new MatchupTeamVM { ModelOdds = 1 - homeModel, Goals = awayGoals },
        BookmakerOdds = books.ToList(),
    };

    private static BookmakerOddsVM Book(string name, double homeOdds, double awayOdds) => new()
    {
        BookmakerName = name,
        HomeOdds = homeOdds,
        AwayOdds = awayOdds,
    };

    [TestMethod]
    public void Underdog_ShouldBetModelPick_WhenBookHasItBelowHalf()
    {
        var game = Game(0.55, 3, 2, Book("DraftKings", 0.45, 0.58));

        var flag = StrategyRules.GetBestBetFlag(game, "underdog", 0);

        flag.Should().NotBeNull();
        flag!.BetHome.Should().BeTrue();
        flag.Edge.Should().BeApproximately(0.10, 1e-9);
    }

    [TestMethod]
    public void Underdog_ShouldSkip_WhenBookAgreesWithModelFavorite()
    {
        var game = Game(0.55, 3, 2, Book("DraftKings", 0.6, 0.43));

        StrategyRules.GetBestBetFlag(game, "underdog", 0).Should().BeNull();
    }

    [TestMethod]
    public void GetBestBetFlag_ShouldIgnoreUnpinnedBooks_AndPickLargestEdge()
    {
        var game = Game(0.6, 3, 2,
            Book("FanDuel", 0.3, 0.72),
            Book("DraftKings", 0.5, 0.52),
            Book("Kalshi", 0.45, 0.56));

        var flag = StrategyRules.GetBestBetFlag(game, "value", 0.05);

        flag!.Bookmaker.BookmakerName.Should().Be("Kalshi");
    }

    [TestMethod]
    public void Payout_Moneyline_ShouldPayImpliedOdds()
    {
        var book = Book("Kalshi", 0.4, 0.62);
        var game = Game(0.55, 3, 2, book);

        var payout = StrategyRules.Payout(game, new BetFlag(true, 0.15, book), "moneyline");

        payout.Should().BeApproximately(1.5, 1e-9);
    }

    [TestMethod]
    public void Payout_Spread_AwayPlusOneAndAHalf_ShouldLose_WhenAwayLosesByTwo()
    {
        var book = new BookmakerOddsVM
        {
            BookmakerName = "DraftKings", HomePoint = -1.5, AwayPoint = 1.5, HomePrice = 150, AwayPrice = -180,
        };
        var game = Game(0.5, 4, 2, book);

        StrategyRules.Payout(game, new BetFlag(false, 1, book), "spread").Should().Be(-1);
    }

    [TestMethod]
    public void Payout_Spread_AwayPlusOneAndAHalf_ShouldWin_WhenAwayLosesByOne()
    {
        var book = new BookmakerOddsVM
        {
            BookmakerName = "DraftKings", HomePoint = -1.5, AwayPoint = 1.5, HomePrice = 150, AwayPrice = -200,
        };
        var game = Game(0.5, 3, 2, book);

        StrategyRules.Payout(game, new BetFlag(false, 1, book), "spread").Should().BeApproximately(0.5, 1e-9);
    }

    [TestMethod]
    public void Payout_Total_ShouldPush_WhenTotalEqualsLine()
    {
        var book = new BookmakerOddsVM { BookmakerName = "DraftKings", OverUnderPoint = 6, OverPrice = -110, UnderPrice = -110 };
        var game = Game(0.5, 4, 2, book);

        StrategyRules.Payout(game, new BetFlag(true, 1, book), "overUnder").Should().Be(0);
    }

    [TestMethod]
    public void PickBest_ShouldOmitBetTypes_WithNoProfitableStrategy()
    {
        // Model always backs the book favorite, which always loses — nothing is profitable.
        var games = Enumerable.Range(0, 150)
            .Select(_ => Game(0.7, 1, 3, Book("DraftKings", 0.6, 0.43)))
            .ToList();

        StrategyBacktester.PickBest(games, 2022, 2025).Should().BeEmpty();
    }

    [TestMethod]
    public void PickBest_ShouldPickUnderdog_WhenModelUnderdogsWin()
    {
        var games = Enumerable.Range(0, 150)
            .Select(_ => Game(0.55, 3, 2, Book("DraftKings", 0.45, 0.58)))
            .ToList();

        var best = StrategyBacktester.PickBest(games, 2022, 2025).Single(b => b.BetType == "moneyline");

        best.Bets.Should().Be(150);
        best.FirstSeason.Should().Be(2022);
        best.LastSeason.Should().Be(2025);
        best.Roi.Should().BeGreaterThan(0);
    }
}
