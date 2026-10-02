using Entities.Types;
using Entities.ViewModels;

namespace WebApi.BusinessLogic.StrategyBacktester;

public record StrategyOption(string Type, string BetType, double[] Thresholds);

public record BetFlag(bool BetHome, double Edge, BookmakerOddsVM Bookmaker);

/// <summary>
/// C# port of the game-card betting rules in frontend/src/utils/bettingStrategies.ts
/// (checkStrategy) and frontend/src/components/GameCard.tsx (getBestBetFlag).
/// Keep the two in sync — the backtest is only meaningful if it scores the same bets
/// the card shows. For spread/total bets, BetHome means home/over.
/// </summary>
public static class StrategyRules
{
    // Only these books are shown on the game card, so only they can back a bet.
    public static readonly string[] PinnedBookmakers = { "DraftKings", "Kalshi" };

    // Mirrors STRATEGY_OPTIONS; strategies without thresholds use a single 0.
    public static readonly StrategyOption[] Options =
    {
        new("value", "moneyline", new[] { 0.03, 0.05, 0.07, 0.1, 0.15 }),
        new("modelWinner", "moneyline", new[] { 0.0 }),
        new("underdog", "moneyline", new[] { 0.0 }),
        new("confidence", "moneyline", new[] { 0.52, 0.55, 0.58, 0.6, 0.65 }),
        new("spreadAll", "spread", new[] { 0.0 }),
        new("spreadValue", "spread", new[] { 0.25, 0.5, 0.75, 1.0, 1.5 }),
        new("totalAll", "overUnder", new[] { 0.0 }),
        new("totalValue", "overUnder", new[] { 0.25, 0.5, 0.75, 1.0, 1.5 }),
    };

    public static BetFlag? CheckStrategy(GameOddsVM game, BookmakerOddsVM bm, string type, double threshold)
    {
        if (game.HomeTeam == null || game.AwayTeam == null)
            return null;

        switch (type)
        {
            case "value":
            case "modelWinner":
            case "underdog":
            case "confidence":
                return CheckMoneyline(game, bm, type, threshold);
            case "spreadAll":
            case "spreadValue":
            {
                if (bm.HomePoint == 0 || game.PredictedSpread == null)
                    return null;
                // PredictedSpread is the predicted margin (home - away); HomePoint is the
                // handicap (opposite sign). Home covers when margin + handicap > 0.
                var coverMargin = game.PredictedSpread.Value + bm.HomePoint;
                var edge = Math.Abs(coverMargin);
                if (type == "spreadValue" && edge < threshold)
                    return null;
                return new BetFlag(coverMargin > 0, edge, bm);
            }
            case "totalAll":
            case "totalValue":
            {
                if (bm.OverUnderPoint == 0 || game.PredictedTotal == null)
                    return null;
                var edge = Math.Abs(game.PredictedTotal.Value - bm.OverUnderPoint);
                if (type == "totalValue" && edge < threshold)
                    return null;
                return new BetFlag(game.PredictedTotal.Value > bm.OverUnderPoint, edge, bm);
            }
            default:
                return null;
        }
    }

    private static BetFlag? CheckMoneyline(GameOddsVM game, BookmakerOddsVM bm, string type, double threshold)
    {
        if (bm.HomeOdds <= 0 || bm.AwayOdds <= 0)
            return null;
        var homeModel = game.HomeTeam!.ModelOdds;
        var awayModel = game.AwayTeam!.ModelOdds;
        if (homeModel == null || awayModel == null)
            return null;

        var homeEdge = homeModel.Value - bm.HomeOdds;
        var awayEdge = awayModel.Value - bm.AwayOdds;

        switch (type)
        {
            case "value":
                if (homeEdge >= threshold && homeEdge >= awayEdge)
                    return new BetFlag(true, homeEdge, bm);
                if (awayEdge >= threshold)
                    return new BetFlag(false, awayEdge, bm);
                return null;
            case "modelWinner":
            {
                var betHome = homeModel.Value >= 0.5;
                return new BetFlag(betHome, betHome ? homeEdge : awayEdge, bm);
            }
            case "underdog":
            {
                var betHome = homeModel.Value >= 0.5;
                var bookProb = betHome ? bm.HomeOdds : bm.AwayOdds;
                if (bookProb >= 0.5)
                    return null;
                return new BetFlag(betHome, betHome ? homeEdge : awayEdge, bm);
            }
            case "confidence":
                if (homeModel.Value >= threshold)
                    return new BetFlag(true, homeEdge, bm);
                if (awayModel.Value >= threshold)
                    return new BetFlag(false, awayEdge, bm);
                return null;
            default:
                return null;
        }
    }

    // The bet the card shows: the pinned bookmaker with the largest edge.
    public static BetFlag? GetBestBetFlag(GameOddsVM game, string type, double threshold)
    {
        BetFlag? best = null;
        foreach (var bm in game.BookmakerOdds)
        {
            if (!PinnedBookmakers.Contains(bm.BookmakerName))
                continue;
            var flag = CheckStrategy(game, bm, type, threshold);
            if (flag != null && (best == null || flag.Edge > best.Edge))
                best = flag;
        }
        return best;
    }

    /// <summary>
    /// Profit in units for a 1-unit stake, graded against the flagged bookmaker's line.
    /// Returns 0 for a push and null when the bet can't be priced.
    /// </summary>
    public static double? Payout(GameOddsVM game, BetFlag bet, string betType)
    {
        var bm = bet.Bookmaker;
        var home = game.HomeTeam!;
        var away = game.AwayTeam!;

        switch (betType)
        {
            case "spread":
            {
                var price = bet.BetHome ? bm.HomePrice : bm.AwayPrice;
                if (price == 0)
                    return null;
                var margin = bet.BetHome ? home.Goals - away.Goals : away.Goals - home.Goals;
                var cover = margin + (bet.BetHome ? bm.HomePoint : bm.AwayPoint);
                return cover > 0 ? AmericanToDecimalPayout(price) : cover < 0 ? -1 : 0;
            }
            case "overUnder":
            {
                var price = bet.BetHome ? bm.OverPrice : bm.UnderPrice;
                if (price == 0)
                    return null;
                var total = home.Goals + away.Goals;
                if (total == bm.OverUnderPoint)
                    return 0;
                var won = bet.BetHome == total > bm.OverUnderPoint;
                return won ? AmericanToDecimalPayout(price) : -1;
            }
            default:
            {
                var prob = bet.BetHome ? bm.HomeOdds : bm.AwayOdds;
                var won = game.Winner == (bet.BetHome ? Winner.HOME : Winner.AWAY);
                return won ? 1 / prob - 1 : -1;
            }
        }
    }

    private static double AmericanToDecimalPayout(int price) =>
        price > 0 ? price / 100.0 : 100.0 / Math.Abs(price);
}
