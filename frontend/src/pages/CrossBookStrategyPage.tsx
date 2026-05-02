import { useState, useMemo, useCallback } from 'react';
import { SeasonSelector } from '../components/SeasonSelector';
import { StrategyCard } from '../components/StrategySummary';
import { StrategyChart } from '../components/StrategyChart';
import { StrategyPicker } from '../components/StrategyPicker';
import { Skeleton, StatCardSkeleton } from '../components/Skeleton';
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

export function CrossBookStrategyPage() {
  const [season, setSeason] = useState(getCurrentSeason());
  const [refBookmakerSel, setRefBookmaker] = useState(MODEL_REFERENCE);
  const [betBookmakerSel, setBetBookmaker] = useState('Kalshi');
  const [showLog, setShowLog] = useState(false);

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

  const refOptions = useMemo(() => [MODEL_REFERENCE, ...bookmakerNames], [bookmakerNames]);

  // Fall back to valid selections when bookmakerNames changes without a re-render cycle
  const refBookmaker =
    bookmakerNames.length > 0 &&
    refBookmakerSel !== MODEL_REFERENCE &&
    !bookmakerNames.includes(refBookmakerSel)
      ? MODEL_REFERENCE
      : refBookmakerSel;
  const betBookmaker =
    bookmakerNames.length > 0 && !bookmakerNames.includes(betBookmakerSel)
      ? bookmakerNames[0]
      : betBookmakerSel;

  const sameBookmaker = refBookmaker === betBookmaker && refBookmaker !== MODEL_REFERENCE;

  const refLabel = refBookmaker === MODEL_REFERENCE ? 'In-House' : refBookmaker;
  const renameLabel = useCallback(
    (label: string) => (refBookmaker !== MODEL_REFERENCE ? label.replace('In-House', refLabel) : label),
    [refBookmaker, refLabel],
  );

  const strategyResult = useMemo(() => {
    if (!games || sameBookmaker) return null;
    const result = runStrategy(games, strategy.type, refBookmaker, betBookmaker, strategy.threshold);
    result.name = renameLabel(result.name);
    return result;
  }, [games, strategy.type, strategy.threshold, refBookmaker, betBookmaker, sameBookmaker, renameLabel]);

  const coverage = useMemo(() => {
    if (!games || sameBookmaker) return null;
    return getCoverage(games, refBookmaker, betBookmaker);
  }, [games, refBookmaker, betBookmaker, sameBookmaker]);

  const logEntries = useMemo(() => {
    if (!games || sameBookmaker) return [];
    return crossBookLog(games, strategy.type, refBookmaker, betBookmaker, strategy.threshold);
  }, [games, strategy.type, strategy.threshold, refBookmaker, betBookmaker, sameBookmaker]);

  const upcomingEntries = useMemo(() => logEntries.filter((e) => e.upcoming), [logEntries]);
  const playedEntries = useMemo(
    () => [...logEntries.filter((e) => !e.upcoming)].reverse(),
    [logEntries],
  );

  const rankings = useMemo(() => {
    if (!games || sameBookmaker) return [];
    return STRATEGY_OPTIONS.map((opt) => {
      const threshold =
        opt.type === strategy.type
          ? strategy.threshold
          : opt.thresholds
            ? opt.thresholds[Math.floor((opt.thresholds.length - 1) / 2)]
            : 0;
      const result = runStrategy(games, opt.type, refBookmaker, betBookmaker, threshold);
      result.name = renameLabel(result.name);
      return { opt, threshold, result };
    })
      .filter((r) => r.result.totalBets > 0)
      .sort((a, b) => b.result.roi - a.result.roi);
  }, [games, refBookmaker, betBookmaker, sameBookmaker, strategy.type, strategy.threshold, renameLabel]);

  const swap = () => {
    setRefBookmaker(betBookmaker);
    setBetBookmaker(refBookmaker);
  };

  const selectClass =
    'px-3 py-2 rounded-lg glass text-sm font-mono font-medium cursor-pointer appearance-none bg-[length:16px] bg-[right_8px_center] bg-no-repeat';
  const selectStyle = {
    backgroundImage: `url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' fill='none' viewBox='0 0 24 24' stroke='%23737373' stroke-width='2'%3E%3Cpath stroke-linecap='round' stroke-linejoin='round' d='M19 9l-7 7-7-7'/%3E%3C/svg%3E")`,
    paddingRight: '2rem',
  };

  const isMoneyline = betType === 'moneyline';

  return (
    <div>
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-3 mb-6">
        <h1 className="font-display text-2xl font-bold tracking-tight">Strategies</h1>
        <div className="flex items-center gap-3">
          <StrategyPicker renameLabel={renameLabel} />
          <SeasonSelector value={season} onChange={setSeason} />
        </div>
      </div>

      {/* Bookmaker selectors */}
      <div className="flex flex-col sm:flex-row items-start sm:items-center gap-3 mb-6">
        <div className="flex items-center gap-2">
          <span className="text-xs font-semibold uppercase tracking-wider text-surface-400 dark:text-surface-500">
            Reference
          </span>
          <select
            value={refBookmaker}
            onChange={(e) => setRefBookmaker(e.target.value)}
            className={selectClass}
            style={selectStyle}
          >
            {refOptions.map((name) => (
              <option key={name} value={name}>
                {name}
              </option>
            ))}
          </select>
        </div>

        <button
          onClick={swap}
          className="px-2.5 py-1.5 rounded-lg glass text-surface-500 dark:text-surface-400 hover:text-surface-700 dark:hover:text-surface-200 transition-colors"
          aria-label="Swap bookmakers"
          title="Swap"
        >
          <svg
            xmlns="http://www.w3.org/2000/svg"
            className="h-4 w-4"
            viewBox="0 0 20 20"
            fill="currentColor"
          >
            <path d="M8 5a1 1 0 100 2h5.586l-1.293 1.293a1 1 0 001.414 1.414l3-3a1 1 0 000-1.414l-3-3a1 1 0 10-1.414 1.414L13.586 5H8zM12 15a1 1 0 100-2H6.414l1.293-1.293a1 1 0 10-1.414-1.414l-3 3a1 1 0 000 1.414l3 3a1 1 0 001.414-1.414L6.414 15H12z" />
          </svg>
        </button>

        <div className="flex items-center gap-2">
          <span className="text-xs font-semibold uppercase tracking-wider text-surface-400 dark:text-surface-500">
            Bet on
          </span>
          <select
            value={betBookmaker}
            onChange={(e) => setBetBookmaker(e.target.value)}
            className={selectClass}
            style={selectStyle}
          >
            {bookmakerNames.map((name) => (
              <option key={name} value={name}>
                {name}
              </option>
            ))}
          </select>
        </div>
      </div>

      {/* Rankings table */}
      {!loading && !error && !sameBookmaker && rankings.length > 0 && (
        <div className="glass rounded-xl p-5 mb-8 overflow-x-auto">
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

      {error && <div className="glass rounded-xl text-center text-red-500 py-8 px-4">{error}</div>}

      {loading && (
        <>
          <div className="glass rounded-xl p-5 mb-8 space-y-3">
            <Skeleton className="h-4 w-32" />
            {Array.from({ length: 6 }).map((_, i) => (
              <Skeleton key={i} className="h-8 w-full rounded-lg" />
            ))}
          </div>
          <div className="grid grid-cols-1 lg:grid-cols-[1fr_2fr] gap-4 mb-8">
            <div className="glass rounded-xl p-5 space-y-3">
              <Skeleton className="h-4 w-24" />
              <div className="grid grid-cols-2 gap-3">
                <StatCardSkeleton />
                <StatCardSkeleton />
                <StatCardSkeleton />
                <StatCardSkeleton />
              </div>
            </div>
            <Skeleton className="h-[300px] w-full rounded-xl" />
          </div>
        </>
      )}

      {sameBookmaker && !loading && (
        <div className="glass rounded-xl text-center text-amber-500 py-8 px-4 mb-8">
          Select two different bookmakers to compare.
        </div>
      )}

      {!loading && !error && !sameBookmaker && strategyResult && (
        <div className="grid grid-cols-1 lg:grid-cols-[1fr_2fr] gap-4 mb-8">
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

      {/* Upcoming value bets */}
      {!loading && !error && !sameBookmaker && upcomingEntries.length > 0 && (
        <div className="glass rounded-xl p-5 mt-8">
          <h2 className="text-xs font-semibold uppercase tracking-wider text-emerald-500 mb-4">
            Upcoming Bets ({upcomingEntries.length})
          </h2>
          <div className="overflow-x-auto">
            <table className="w-full text-xs font-mono">
              <thead>
                <tr className="text-left text-surface-400 dark:text-surface-500 border-b border-surface-200 dark:border-white/[0.06]">
                  <th className="pb-2 pr-3">Date</th>
                  <th className="pb-2 pr-3">Matchup</th>
                  <th className="pb-2 pr-3">Bet</th>
                  <th className="pb-2 pr-3 text-right">Ref</th>
                  <th className="pb-2 pr-3 text-right">Market</th>
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
                    <td className="py-1.5 pr-3 text-right text-surface-500 dark:text-surface-400">
                      {isMoneyline ? (e.refValue * 100).toFixed(1) + '%' : e.refValue.toFixed(1)}
                    </td>
                    <td className="py-1.5 pr-3 text-right text-surface-500 dark:text-surface-400">
                      {isMoneyline ? (e.betValue * 100).toFixed(1) + '%' : e.betValue.toFixed(1)}
                    </td>
                    <td className="py-1.5 text-right font-semibold text-emerald-500">
                      {isMoneyline ? (e.edge * 100).toFixed(1) + '%' : e.edge.toFixed(2)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Bet log */}
      {!loading && !error && !sameBookmaker && playedEntries.length > 0 && (
        <div className="glass rounded-xl p-5 mt-8">
          <button
            onClick={() => setShowLog(!showLog)}
            className="w-full flex items-center justify-between cursor-pointer"
          >
            <h2 className="text-xs font-semibold uppercase tracking-wider text-surface-400 dark:text-surface-500">
              Bet Log ({playedEntries.length})
            </h2>
            <svg
              xmlns="http://www.w3.org/2000/svg"
              className={`h-4 w-4 text-surface-400 transition-transform ${showLog ? 'rotate-180' : ''}`}
              viewBox="0 0 20 20"
              fill="currentColor"
            >
              <path
                fillRule="evenodd"
                d="M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z"
                clipRule="evenodd"
              />
            </svg>
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

      <div className="glass rounded-xl p-6 mt-8">
        <h2 className="text-xs font-semibold uppercase tracking-wider text-surface-400 dark:text-surface-500 mb-4">
          How It Works
        </h2>
        <div className="space-y-4 text-sm text-surface-600 dark:text-surface-400 leading-relaxed">
          <p>
            This page simulates flat{' '}
            <strong className="text-surface-800 dark:text-surface-200">1-unit bets</strong> using
            the selected reference's predictions against a betting market's odds. The{' '}
            <strong className="text-surface-800 dark:text-surface-200">reference</strong> can be the
            In-House model or any bookmaker (with vig removed). You bet on the{' '}
            <strong className="text-surface-800 dark:text-surface-200">betting market</strong> when
            the strategy criteria are met.
          </p>
          {betType === 'moneyline' && (
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <h3 className="font-semibold text-surface-800 dark:text-surface-200 mb-1">
                  Value Bets
                </h3>
                <p>
                  Bets when the reference's probability exceeds the betting market's implied
                  probability by at least the edge threshold.
                </p>
              </div>
              <div>
                <h3 className="font-semibold text-surface-800 dark:text-surface-200 mb-1">
                  In-House Winner
                </h3>
                <p>
                  Bets every game on whichever team the reference gives {'>'}50% chance to win.
                  Baseline strategy — no edge filter.
                </p>
              </div>
              <div>
                <h3 className="font-semibold text-surface-800 dark:text-surface-200 mb-1">
                  In-House Underdog
                </h3>
                <p>
                  Only bets when the reference's pick is the betting market's underdog (implied
                  probability {'<'}50%). Contrarian bets with longer odds.
                </p>
              </div>
              <div>
                <h3 className="font-semibold text-surface-800 dark:text-surface-200 mb-1">
                  Confidence
                </h3>
                <p>
                  Only bets when the reference's confidence in its pick exceeds the threshold. Skips
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
                  Bets every game against the spread. If the reference's predicted margin differs
                  from the betting market's line, bet the side the reference favors.
                </p>
              </div>
              <div>
                <h3 className="font-semibold text-surface-800 dark:text-surface-200 mb-1">
                  Spread Value
                </h3>
                <p>
                  Only bets when the reference's predicted spread differs from the betting market's
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
                  Bets every game on over or under. If the reference predicts a higher total than
                  the betting market's line, bet over; if lower, bet under.
                </p>
              </div>
              <div>
                <h3 className="font-semibold text-surface-800 dark:text-surface-200 mb-1">
                  O/U Value
                </h3>
                <p>
                  Only bets when the reference's predicted total differs from the betting market's
                  line by at least the edge threshold (in goals).
                </p>
              </div>
            </div>
          )}
          <div className="border-t border-surface-200 dark:border-white/[0.06] pt-4 text-xs text-surface-400 dark:text-surface-500">
            <p>
              <strong>Win Rate</strong> = wins / total bets. <strong>P/L</strong> = total units won
              minus total units lost. <strong>ROI</strong> = P/L / total bets as a percentage. The
              chart shows cumulative P/L over the season; above the dashed line is profit.
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
