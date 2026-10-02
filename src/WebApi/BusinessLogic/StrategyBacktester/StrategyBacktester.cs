using Entities.Types;
using Entities.ViewModels;
using WebApi.BusinessLogic.GameOddsGetter;

namespace WebApi.BusinessLogic.StrategyBacktester;

public class StrategyBacktester : IStrategyBacktester
{
    public const int SEASONS_TO_TEST = 4;
    public const int MIN_BETS = 100;

    private readonly IGameOddsGetter _gameOddsGetter;

    public StrategyBacktester(IGameOddsGetter gameOddsGetter)
    {
        _gameOddsGetter = gameOddsGetter;
    }

    // Backtests every strategy/threshold over the last completed seasons and returns the
    // best one per bet type. Bet types with no profitable strategy are left out.
    public async Task<IEnumerable<BestStrategyVM>> GetBestStrategies(int seasonStartYear)
    {
        var firstSeason = seasonStartYear - SEASONS_TO_TEST;
        var lastSeason = seasonStartYear - 1;

        var games = new List<GameOddsVM>();
        for (var season = firstSeason; season <= lastSeason; season++)
        {
            var range = new DateRange
            {
                StartDate = new DateTime(season, 9, 1),
                EndDate = new DateTime(season + 1, 7, 31)
            };
            games.AddRange(await _gameOddsGetter.GetGameOddsInDateRange(range, season));
        }

        return PickBest(games, firstSeason, lastSeason);
    }

    public static IEnumerable<BestStrategyVM> PickBest(IReadOnlyCollection<GameOddsVM> games, int firstSeason, int lastSeason)
    {
        var played = games
            .Where(g => g.HasBeenPlayed && g.HomeTeam != null && g.AwayTeam != null)
            .ToList();

        var results = StrategyRules.Options
            .SelectMany(opt => opt.Thresholds.Select(t => Backtest(played, opt, t)))
            .Where(r => r.Bets >= MIN_BETS && r.Roi > 0);

        return results
            .GroupBy(r => r.BetType)
            .Select(g => g.OrderByDescending(r => r.Roi).First())
            .Select(r =>
            {
                r.FirstSeason = firstSeason;
                r.LastSeason = lastSeason;
                return r;
            })
            .ToList();
    }

    private static BestStrategyVM Backtest(List<GameOddsVM> games, StrategyOption opt, double threshold)
    {
        int bets = 0, wins = 0;
        double profit = 0;
        foreach (var game in games)
        {
            var flag = StrategyRules.GetBestBetFlag(game, opt.Type, threshold);
            if (flag == null)
                continue;
            var payout = StrategyRules.Payout(game, flag, opt.BetType);
            if (payout == null)
                continue;
            bets++;
            if (payout > 0)
                wins++;
            profit += payout.Value;
        }

        return new BestStrategyVM
        {
            BetType = opt.BetType,
            StrategyType = opt.Type,
            Threshold = threshold,
            Bets = bets,
            Wins = wins,
            Roi = bets > 0 ? Math.Round(profit / bets * 100, 2) : 0,
        };
    }
}
