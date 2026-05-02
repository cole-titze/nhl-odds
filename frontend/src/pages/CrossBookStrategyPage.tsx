import { useState, useMemo } from 'react';
import { SeasonSelector } from '../components/SeasonSelector';
import { StrategyCard } from '../components/StrategySummary';
import { StrategyChart } from '../components/StrategyChart';
import { Skeleton } from '../components/Skeleton';
import { useFetch } from '../hooks/useFetch';
import { useStrategy } from '../contexts/StrategyContext';
import { getGameOddsInDateRange } from '../api/gameOdds';
import { getCurrentSeason } from '../utils/season';
import { STRATEGY_OPTIONS } from '../utils/bettingStrategies';
import {
  MODEL_REFERENCE,
  runStrategy,
  crossBookLog,
  getCoverage,
} from '../utils/crossBookStrategy';
import { formatShortDate } from '../utils/dates';

const MARKET_OPTIONS = ['Kalshi', 'DraftKings'];

interface MarketPair { ref: string; bet: string; label: string }

export function CrossBookStrategyPage() {
  const [season, setSeason] = useState(getCurrentSeason());
  const [selectedPair, setSelectedPair] = useState<MarketPair>({
    ref: MODEL_REFERENCE,
    bet: 'Kalshi',
    label: 'In-House vs Kalshi',
  });
  const [showLog, setShowLog] = useState(false);
  const [showChart, setShowChart] = useState(false);
  const [showHowItWorks, setShowHowItWorks] = useState(false);

  const { strategy, setStrategy } = useStrategy();
  const strategyOption = STRATEGY_OPTIONS.find((o) => o.type === strategy.type);
  const betType = strategyOption?.betType ?? 'moneyline';

  const startDate = `${season}-09-01`;
  const endDate = `${season + 1}-07-31`;

  const {
    data: games,
    loading,
    error,
  } = useFetch(
    () => getGameOddsInDateRange(startDate, endDate, season),
    [startDate, endDate, season],
  );

  const bookmakerNames = useMemo(() => {
    if (!games) return [];
    const names = new Set<string>();
    for (const g of games) {
      for (const b of g.bookmakerOdds ?? []) {
        if (b.homeOdds > 0 && b.awayOdds > 0) names.add(b.bookmakerName);
      }
    }
    return Array.from(names).sort();
  }, [games]);

  const marketPairs = useMemo((): MarketPair[] => {
    const pairs: MarketPair[] = [];
    for (const m of MARKET_OPTIONS) {
      if (bookmakerNames.includes(m)) {
        pairs.push({ ref: MODEL_REFERENCE, bet: m, label: `In-House vs ${m}` });
      }
    }
    if (MARKET_OPTIONS.every((m) => bookmakerNames.includes(m))) {
      pairs.push({ ref: MARKET_OPTIONS[0], bet: MARKET_OPTIONS[1], label: `${MARKET_OPTIONS[0]} vs ${MARKET_OPTIONS[1]}` });
      pairs.push({ ref: MARKET_OPTIONS[1], bet: MARKET_OPTIONS[0], label: `${MARKET_OPTIONS[1]} vs ${MARKET_OPTIONS[0]}` });
    }
    return pairs;
  }, [bookmakerNames]);

  const activePair = marketPairs.find((p) => p.label === selectedPair.label) ?? marketPairs[0] ?? selectedPair;
  const refBookmaker = activePair.ref;
  const betBookmaker = activePair.bet;

  const strategyResult = useMemo(() => {
    if (!games) return null;
    return runStrategy(games, strategy.type, refBookmaker, betBookmaker, strategy.threshold);
  }, [games, strategy.type, strategy.threshold, refBookmaker, betBookmaker]);

  const coverage = useMemo(() => {
    if (!games) return null;
    return getCoverage(games, refBookmaker, betBookmaker);
  }, [games, refBookmaker, betBookmaker]);

  const logEntries = useMemo(() => {
    if (!games) return [];
    return crossBookLog(games, strategy.type, refBookmaker, betBookmaker, strategy.threshold);
  }, [games, strategy.type, strategy.threshold, refBookmaker, betBookmaker]);

  const upcomingEntries = useMemo(() => logEntries.filter((e) => e.upcoming), [logEntries]);
  const playedEntries = useMemo(
    () => [...logEntries.filter((e) => !e.upcoming)].reverse(),
    [logEntries],
  );

  const rankings = useMemo(() => {
    if (!games) return [];
    return STRATEGY_OPTIONS.map((opt) => {
      const threshold =
        opt.type === strategy.type
          ? strategy.threshold
          : opt.thresholds
            ? opt.thresholds[Math.floor((opt.thresholds.length - 1) / 2)]
            : 0;
      const result = runStrategy(games, opt.type, refBookmaker, betBookmaker, threshold);
      return { opt, threshold, result };
    })
      .filter((r) => r.result.totalBets > 0)
      .sort((a, b) => b.result.roi - a.result.roi);
  }, [games, refBookmaker, betBookmaker, strategy.type, strategy.threshold]);

  const isMoneyline = betType === 'moneyline';

  const chevron = (open: boolean) => (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      className={`h-4 w-4 text-surface-400 transition-transform ${open ? 'rotate-180' : ''}`}
      viewBox="0 0 20 20"
      fill="currentColor"
    >
      <path
        fillRule="evenodd"
        d="M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z"
        clipRule="evenodd"
      />
    </svg>
  );

  return (
    <div>
      {/* Header */}
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-3 mb-6">
        <h1 className="font-display text-2xl font-bold tracking-tight">Strategies</h1>
        <SeasonSelector value={season} onChange={setSeason} />
      </div>

      {/* Betting market toggle */}
      {!loading && marketPairs.length > 0 && (
        <div className="flex items-center gap-3 mb-6 flex-wrap">
          <div className="flex rounded-lg glass p-1 gap-1 flex-wrap">
            {marketPairs.map((pair) => (
              <button
                key={pair.label}
                onClick={() => setSelectedPair(pair)}
                className={`px-3 py-1 rounded-md text-sm font-medium transition-colors ${
                  activePair.label === pair.label
                    ? 'bg-accent-500 text-white'
                    : 'text-surface-500 dark:text-surface-400 hover:text-surface-700 dark:hover:text-surface-200'
                }`}
              >
                {pair.label}
              </button>
            ))}
          </div>
        </div>
      )}

      {error && (
        <div className="glass rounded-xl text-center text-red-500 py-8 px-4 mb-8">{error}</div>
      )}

      {loading && (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-4 mb-8">
          <div className="glass rounded-xl p-5 space-y-3">
            <Skeleton className="h-4 w-32" />
            {Array.from({ length: 7 }).map((_, i) => (
              <Skeleton key={i} className="h-7 w-full rounded-lg" />
            ))}
          </div>
          <div className="glass rounded-xl p-5 space-y-3">
            <Skeleton className="h-4 w-32" />
            {Array.from({ length: 4 }).map((_, i) => (
              <Skeleton key={i} className="h-7 w-full rounded-lg" />
            ))}
          </div>
        </div>
      )}

      {/* Rankings + Upcoming side by side */}
      {!loading && !error && (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-4 mb-8 items-start">

          {/* Rankings table — click a row to select strategy */}
          {rankings.length > 0 && (
          <div className="glass rounded-xl p-5 overflow-x-auto">
          <h2 className="text-xs font-semibold uppercase tracking-wider text-surface-400 dark:text-surface-500 mb-3">
            Strategy Rankings
          </h2>
          <table className="w-full text-xs font-mono">
            <thead>
              <tr className="text-left text-surface-400 dark:text-surface-500 border-b border-surface-200 dark:border-white/[0.06]">
                <th className="pb-2 pr-4">Strategy</th>
                <th className="pb-2 pr-4">Type</th>
                <th className="pb-2 pr-4 text-right">Bets</th>
                <th className="pb-2 pr-4 text-right">Win%</th>
                <th className="pb-2 pr-4 text-right">P/L</th>
                <th className="pb-2 text-right">ROI</th>
              </tr>
            </thead>
            <tbody>
              {rankings.map(({ opt, threshold, result }) => {
                const isSelected = strategy.type === opt.type;
                const thresholdLabel = opt.thresholds
                  ? opt.thresholdFormat === 'goals'
                    ? ` ${threshold}`
                    : ` ${(threshold * 100).toFixed(0)}%`
                  : '';
                const typeLabel =
                  opt.betType === 'moneyline' ? 'ML' : opt.betType === 'spread' ? 'Spread' : 'O/U';
                return (
                  <tr
                    key={opt.type}
                    onClick={() => setStrategy({ type: opt.type, threshold })}
                    className={`border-b border-surface-200/50 dark:border-white/[0.03] cursor-pointer transition-colors ${
                      isSelected
                        ? 'bg-accent-500/10'
                        : 'hover:bg-surface-100 dark:hover:bg-white/[0.03]'
                    }`}
                  >
                    <td
                      className={`py-2 pr-4 font-semibold ${isSelected ? 'text-accent-500' : 'text-surface-800 dark:text-surface-200'}`}
                    >
                      {result.name}
                      {thresholdLabel}
                    </td>
                    <td className="py-2 pr-4 text-surface-500 dark:text-surface-400">
                      {typeLabel}
                    </td>
                    <td className="py-2 pr-4 text-right text-surface-500 dark:text-surface-400">
                      {result.totalBets}
                    </td>
                    <td className="py-2 pr-4 text-right text-surface-500 dark:text-surface-400">
                      {(result.winRate * 100).toFixed(1)}%
                    </td>
                    <td
                      className={`py-2 pr-4 text-right font-semibold ${result.totalPL >= 0 ? 'text-emerald-500' : 'text-red-500'}`}
                    >
                      {result.totalPL >= 0 ? '+' : ''}
                      {result.totalPL.toFixed(2)}u
                    </td>
                    <td
                      className={`py-2 text-right font-semibold ${result.roi >= 0 ? 'text-emerald-500' : 'text-red-500'}`}
                    >
                      {result.roi >= 0 ? '+' : ''}
                      {result.roi.toFixed(1)}%
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
          </div>
          )}

          {/* Upcoming bets */}
          <div className="glass rounded-xl p-5">
            <h2 className={`text-xs font-semibold uppercase tracking-wider mb-4 ${upcomingEntries.length > 0 ? 'text-emerald-500' : 'text-surface-400 dark:text-surface-500'}`}>
              Upcoming Bets{upcomingEntries.length > 0 ? ` (${upcomingEntries.length})` : ''}
            </h2>
            {upcomingEntries.length === 0 ? (
              <div className="flex items-center justify-center h-20 text-xs text-surface-400 dark:text-surface-500">
                No upcoming bets for this strategy
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-xs font-mono">
                  <thead>
                    <tr className="text-left text-surface-400 dark:text-surface-500 border-b border-surface-200 dark:border-white/[0.06]">
                      <th className="pb-2 pr-3">Date</th>
                      <th className="pb-2 pr-3">Matchup</th>
                      <th className="pb-2 pr-3">Bet</th>
                      <th className="pb-2 text-right">Edge</th>
                    </tr>
                  </thead>
                  <tbody>
                    {upcomingEntries.map((e) => (
                      <tr
                        key={e.gameId}
                        className="border-b border-surface-200/50 dark:border-white/[0.03]"
                      >
                        <td className="py-1.5 pr-3 text-surface-500 dark:text-surface-400">
                          {formatShortDate(e.gameDate)}
                        </td>
                        <td className="py-1.5 pr-3 text-surface-800 dark:text-surface-200">
                          {e.awayTeam} @ {e.homeTeam}
                        </td>
                        <td className="py-1.5 pr-3 font-semibold text-emerald-500">{e.betSide}</td>
                        <td className="py-1.5 text-right font-semibold text-emerald-500">
                          {isMoneyline ? (e.edge * 100).toFixed(1) + '%' : e.edge.toFixed(2)}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>

        </div>
      )}

      {/* Strategy stats + chart (collapsible) */}
      {!loading && !error && strategyResult && (
        <div className="glass rounded-xl p-5 mb-8">
          <button
            onClick={() => setShowChart(!showChart)}
            className="w-full flex items-center justify-between cursor-pointer"
          >
            <h2 className="text-xs font-semibold uppercase tracking-wider text-surface-400 dark:text-surface-500">
              {strategyResult.name} — Performance
            </h2>
            {chevron(showChart)}
          </button>
          {showChart && (
            <div className="grid grid-cols-1 lg:grid-cols-[1fr_2fr] gap-4 mt-4">
              <div>
                <StrategyCard result={strategyResult} />
                {coverage && (
                  <p className="text-xs text-surface-400 dark:text-surface-500 mt-2">
                    {coverage.matched} of {coverage.total} played games had odds from both sources
                  </p>
                )}
              </div>
              <StrategyChart results={[strategyResult]} />
            </div>
          )}
        </div>
      )}

      {/* Bet log (collapsible) */}
      {!loading && !error && playedEntries.length > 0 && (
        <div className="glass rounded-xl p-5 mb-8">
          <button
            onClick={() => setShowLog(!showLog)}
            className="w-full flex items-center justify-between cursor-pointer"
          >
            <h2 className="text-xs font-semibold uppercase tracking-wider text-surface-400 dark:text-surface-500">
              Bet Log ({playedEntries.length})
            </h2>
            {chevron(showLog)}
          </button>
          {showLog && (
            <div className="overflow-x-auto mt-4">
              <table className="w-full text-xs font-mono">
                <thead>
                  <tr className="text-left text-surface-400 dark:text-surface-500 border-b border-surface-200 dark:border-white/[0.06]">
                    <th className="pb-2 pr-3">Date</th>
                    <th className="pb-2 pr-3">Matchup</th>
                    <th className="pb-2 pr-3">Bet</th>
                    <th className="pb-2 pr-3 text-right">Ref</th>
                    <th className="pb-2 pr-3 text-right">Market</th>
                    <th className="pb-2 pr-3 text-right">Edge</th>
                    <th className="pb-2 pr-3 text-right">Result</th>
                    <th className="pb-2 text-right">P/L</th>
                  </tr>
                </thead>
                <tbody>
                  {playedEntries.map((e) => (
                    <tr
                      key={e.gameId}
                      className="border-b border-surface-200/50 dark:border-white/[0.03]"
                    >
                      <td className="py-1.5 pr-3 text-surface-500 dark:text-surface-400">
                        {formatShortDate(e.gameDate)}
                      </td>
                      <td className="py-1.5 pr-3 text-surface-800 dark:text-surface-200">
                        {e.awayTeam} @ {e.homeTeam}
                      </td>
                      <td className="py-1.5 pr-3 font-semibold text-surface-800 dark:text-surface-200">
                        {e.betSide}
                      </td>
                      <td className="py-1.5 pr-3 text-right text-surface-500 dark:text-surface-400">
                        {isMoneyline ? (e.refValue * 100).toFixed(1) + '%' : e.refValue.toFixed(1)}
                      </td>
                      <td className="py-1.5 pr-3 text-right text-surface-500 dark:text-surface-400">
                        {isMoneyline ? (e.betValue * 100).toFixed(1) + '%' : e.betValue.toFixed(1)}
                      </td>
                      <td className="py-1.5 pr-3 text-right text-surface-500 dark:text-surface-400">
                        {isMoneyline ? (e.edge * 100).toFixed(1) + '%' : e.edge.toFixed(2)}
                      </td>
                      <td
                        className={`py-1.5 pr-3 text-right font-semibold ${e.won ? 'text-emerald-500' : 'text-red-500'}`}
                      >
                        {e.won ? 'W' : 'L'}
                      </td>
                      <td
                        className={`py-1.5 text-right font-semibold ${e.payout! >= 0 ? 'text-emerald-500' : 'text-red-500'}`}
                      >
                        {e.payout! >= 0 ? '+' : ''}
                        {e.payout!.toFixed(2)}u
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}

      {/* How It Works (collapsible) */}
      <div className="glass rounded-xl p-6 mb-8">
        <button
          onClick={() => setShowHowItWorks(!showHowItWorks)}
          className="w-full flex items-center justify-between cursor-pointer"
        >
          <h2 className="text-xs font-semibold uppercase tracking-wider text-surface-400 dark:text-surface-500">
            How It Works
          </h2>
          {chevron(showHowItWorks)}
        </button>
        {showHowItWorks && (
          <div className="space-y-4 text-sm text-surface-600 dark:text-surface-400 leading-relaxed mt-4">
            <p>
              This page simulates flat{' '}
              <strong className="text-surface-800 dark:text-surface-200">1-unit bets</strong> using
              the In-House model's predictions against the selected betting market's odds. You bet
              when the strategy criteria are met.
            </p>
            {betType === 'moneyline' && (
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <h3 className="font-semibold text-surface-800 dark:text-surface-200 mb-1">
                    Value Bets
                  </h3>
                  <p>
                    Bets when the model's probability exceeds the betting market's implied
                    probability by at least the edge threshold.
                  </p>
                </div>
                <div>
                  <h3 className="font-semibold text-surface-800 dark:text-surface-200 mb-1">
                    In-House Winner
                  </h3>
                  <p>
                    Bets every game on whichever team the model gives {'>'}50% chance to win.
                    Baseline strategy — no edge filter.
                  </p>
                </div>
                <div>
                  <h3 className="font-semibold text-surface-800 dark:text-surface-200 mb-1">
                    In-House Underdog
                  </h3>
                  <p>
                    Only bets when the model's pick is the betting market's underdog (implied
                    probability {'<'}50%). Contrarian bets with longer odds.
                  </p>
                </div>
                <div>
                  <h3 className="font-semibold text-surface-800 dark:text-surface-200 mb-1">
                    Confidence
                  </h3>
                  <p>
                    Only bets when the model's confidence in its pick exceeds the threshold. Skips
                    coin-flip games.
                  </p>
                </div>
              </div>
            )}
            {betType === 'spread' && (
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <h3 className="font-semibold text-surface-800 dark:text-surface-200 mb-1">
                    Spread Bet
                  </h3>
                  <p>
                    Bets every game against the spread. If the model's predicted margin differs
                    from the betting market's line, bet the side the model favors.
                  </p>
                </div>
                <div>
                  <h3 className="font-semibold text-surface-800 dark:text-surface-200 mb-1">
                    Spread Value
                  </h3>
                  <p>
                    Only bets when the model's predicted spread differs from the betting market's
                    line by at least the edge threshold (in goals).
                  </p>
                </div>
              </div>
            )}
            {betType === 'overUnder' && (
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <h3 className="font-semibold text-surface-800 dark:text-surface-200 mb-1">
                    O/U Bet
                  </h3>
                  <p>
                    Bets every game on over or under. If the model predicts a higher total than the
                    betting market's line, bet over; if lower, bet under.
                  </p>
                </div>
                <div>
                  <h3 className="font-semibold text-surface-800 dark:text-surface-200 mb-1">
                    O/U Value
                  </h3>
                  <p>
                    Only bets when the model's predicted total differs from the betting market's
                    line by at least the edge threshold (in goals).
                  </p>
                </div>
              </div>
            )}
            <div className="border-t border-surface-200 dark:border-white/[0.06] pt-4 text-xs text-surface-400 dark:text-surface-500">
              <p>
                <strong>Win Rate</strong> = wins / total bets. <strong>P/L</strong> = total units
                won minus total units lost. <strong>ROI</strong> = P/L / total bets as a percentage.
                The chart shows cumulative P/L over the season; above the dashed line is profit.
              </p>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
