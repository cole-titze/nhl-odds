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
    // best one per bet type. Only strategies that were profitable in every season they bet
    // qualify; bet types with no such strategy are left out.
    public async Task<IEnumerable<BestStrategyVM>> GetBestStrategies(int seasonStartYear)
    {
        var games = await LoadTestSeasons(seasonStartYear);
        return PickBest(games, seasonStartYear - SEASONS_TO_TEST, seasonStartYear - 1);
    }

    private async Task<List<GameOddsVM>> LoadTestSeasons(int seasonStartYear)
    {
        var games = new List<GameOddsVM>();
        for (var season = seasonStartYear - SEASONS_TO_TEST; season < seasonStartYear; season++)
        {
            var range = new DateRange
            {
                StartDate = new DateTime(season, 9, 1),
                EndDate = new DateTime(season + 1, 7, 31)
            };
            games.AddRange(await _gameOddsGetter.GetGameOddsInDateRange(range, season));
        }
        return games;
    }

    public static IEnumerable<BestStrategyVM> PickBest(IReadOnlyCollection<GameOddsVM> games, int firstSeason, int lastSeason)
    {
        var played = Played(games);

        var results = StrategyRules.Options
            .SelectMany(opt => opt.Thresholds.Select(t => Backtest(played, opt, t)))
            .Where(r => r.Bets >= MIN_BETS && r.ProfitBySeason.Count > 0 && r.ProfitBySeason.Values.All(p => p > 0))
            .Select(r => new BestStrategyVM
            {
                BetType = r.Opt.BetType,
                StrategyType = r.Opt.Type,
                Threshold = r.Threshold,
                Bets = r.Bets,
                Wins = r.Wins,
                Roi = Math.Round(r.Profit / r.Bets * 100, 2),
                FirstSeason = firstSeason,
                LastSeason = lastSeason,
            });

        return results
            .GroupBy(r => r.BetType)
            .Select(g => g.OrderByDescending(r => r.Roi).First())
            .ToList();
    }

    private static List<GameOddsVM> Played(IEnumerable<GameOddsVM> games) => games
        .Where(g => g.HasBeenPlayed && g.HomeTeam != null && g.AwayTeam != null)
        .ToList();

    private record BacktestResult(
        StrategyOption Opt,
        double Threshold,
        int Bets,
        int Wins,
        double Profit,
        Dictionary<int, double> ProfitBySeason);

    private static BacktestResult Backtest(List<GameOddsVM> games, StrategyOption opt, double threshold)
    {
        int bets = 0, wins = 0;
        double profit = 0;
        var profitBySeason = new Dictionary<int, double>();
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
            // The first 4 digits of a game ID are the season start year
            var season = game.Id / 1_000_000;
            profitBySeason[season] = profitBySeason.GetValueOrDefault(season) + payout.Value;
        }

        return new BacktestResult(opt, threshold, bets, wins, profit, profitBySeason);
    }
}
