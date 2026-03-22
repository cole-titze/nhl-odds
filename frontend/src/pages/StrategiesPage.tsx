import { useState, useMemo } from 'react';
import { SeasonSelector } from '../components/SeasonSelector';
import { StrategyCard } from '../components/StrategySummary';
import { StrategyChart } from '../components/StrategyChart';
import { StatCardSkeleton } from '../components/Skeleton';
import { useFetch } from '../hooks/useFetch';
import { getGameOddsInDateRange } from '../api/gameOdds';
import { getCurrentSeason } from '../utils/season';
import {
  alwaysBetModelWinner,
  valueBetsOnly,
  modelUnderdogPicks,
  confidenceFilter,
  spreadAlwaysBet,
  spreadValueOnly,
  totalAlwaysBet,
  totalValueOnly,
  type StrategyResult,
} from '../utils/bettingStrategies';

type BetType = 'moneyline' | 'spread' | 'overUnder';

const VALUE_THRESHOLDS = [0.03, 0.05, 0.07, 0.1, 0.15];
const CONFIDENCE_OPTIONS = [0.52, 0.55, 0.58, 0.6, 0.65];
const SPREAD_EDGE_OPTIONS = [0.25, 0.5, 0.75, 1.0, 1.5];
const TOTAL_EDGE_OPTIONS = [0.25, 0.5, 0.75, 1.0, 1.5];

export function StrategiesPage() {
  const [season, setSeason] = useState(getCurrentSeason());
  const [bookmaker, setBookmaker] = useState('DraftKings');
  const [betType, setBetType] = useState<BetType>('moneyline');
  const [valueThreshold, setValueThreshold] = useState(0.05);
  const [confidenceMin, setConfidenceMin] = useState(0.55);
  const [spreadEdge, setSpreadEdge] = useState(0.5);
  const [totalEdge, setTotalEdge] = useState(0.5);

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

  // Moneyline strategies
  const modelWinner = useMemo(
    () => games && alwaysBetModelWinner(games, bookmaker),
    [games, bookmaker],
  );
  const underdog = useMemo(() => games && modelUnderdogPicks(games, bookmaker), [games, bookmaker]);
  const value = useMemo(
    () => games && valueBetsOnly(games, bookmaker, valueThreshold),
    [games, bookmaker, valueThreshold],
  );
  const confidence = useMemo(
    () => games && confidenceFilter(games, bookmaker, confidenceMin),
    [games, bookmaker, confidenceMin],
  );

  // Spread strategies
  const spreadAll = useMemo(() => games && spreadAlwaysBet(games, bookmaker), [games, bookmaker]);
  const spreadVal = useMemo(
    () => games && spreadValueOnly(games, bookmaker, spreadEdge),
    [games, bookmaker, spreadEdge],
  );

  // Over/Under strategies
  const totalAll = useMemo(() => games && totalAlwaysBet(games, bookmaker), [games, bookmaker]);
  const totalVal = useMemo(
    () => games && totalValueOnly(games, bookmaker, totalEdge),
    [games, bookmaker, totalEdge],
  );

  const allStrategies = useMemo(() => {
    let list: (StrategyResult | null)[] = [];
    if (betType === 'moneyline') list = [modelWinner, underdog, value, confidence];
    else if (betType === 'spread') list = [spreadAll, spreadVal];
    else list = [totalAll, totalVal];
    return list.filter((s): s is StrategyResult => s != null);
  }, [betType, modelWinner, underdog, value, confidence, spreadAll, spreadVal, totalAll, totalVal]);

  const pillClass = (active: boolean) =>
    `px-3 py-1 text-xs font-medium rounded-full transition-colors ${
      active
        ? 'bg-accent-500 text-white'
        : 'glass text-surface-500 dark:text-surface-400 hover:text-surface-700 dark:hover:text-surface-200'
    }`;

  return (
    <div>
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-3 mb-8">
        <h1 className="font-display text-2xl font-bold tracking-tight">Strategies</h1>
        <div className="flex items-center gap-3">
          <div className="flex gap-1">
            {(['moneyline', 'spread', 'overUnder'] as const).map((type) => (
              <button
                key={type}
                onClick={() => setBetType(type)}
                className={pillClass(betType === type)}
              >
                {type === 'moneyline' ? 'Moneyline' : type === 'spread' ? 'Spread' : 'Over/Under'}
              </button>
            ))}
          </div>
          <select
            value={bookmaker}
            onChange={(e) => setBookmaker(e.target.value)}
            className="px-3 py-2 rounded-lg glass text-sm font-mono font-medium cursor-pointer appearance-none bg-[length:16px] bg-[right_8px_center] bg-no-repeat"
            style={{
              backgroundImage: `url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' fill='none' viewBox='0 0 24 24' stroke='%23737373' stroke-width='2'%3E%3Cpath stroke-linecap='round' stroke-linejoin='round' d='M19 9l-7 7-7-7'/%3E%3C/svg%3E")`,
              paddingRight: '2rem',
            }}
          >
            {bookmakerNames.map((name) => (
              <option key={name} value={name}>
                {name}
              </option>
            ))}
          </select>
          <SeasonSelector value={season} onChange={setSeason} />
        </div>
      </div>

      {error && <div className="glass rounded-xl text-center text-red-500 py-8 px-4">{error}</div>}

      {loading && (
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 mb-8">
          {Array.from({ length: 4 }).map((_, i) => (
            <StatCardSkeleton key={i} />
          ))}
        </div>
      )}

      {!loading &&
        !error &&
        betType === 'moneyline' &&
        modelWinner &&
        underdog &&
        value &&
        confidence && (
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 mb-8">
            <StrategyCard result={modelWinner} />
            <StrategyCard result={underdog} />
            <StrategyCard result={value}>
              {VALUE_THRESHOLDS.map((t) => (
                <button
                  key={t}
                  onClick={() => setValueThreshold(t)}
                  className={`px-2 py-0.5 text-xs font-medium rounded-full transition-colors ${
                    valueThreshold === t
                      ? 'bg-accent-500 text-white'
                      : 'glass text-surface-500 dark:text-surface-400 hover:text-surface-700 dark:hover:text-surface-200'
                  }`}
                >
                  {(t * 100).toFixed(0)}%
                </button>
              ))}
            </StrategyCard>
            <StrategyCard result={confidence}>
              {CONFIDENCE_OPTIONS.map((c) => (
                <button
                  key={c}
                  onClick={() => setConfidenceMin(c)}
                  className={`px-2 py-0.5 text-xs font-medium rounded-full transition-colors ${
                    confidenceMin === c
                      ? 'bg-accent-500 text-white'
                      : 'glass text-surface-500 dark:text-surface-400 hover:text-surface-700 dark:hover:text-surface-200'
                  }`}
                >
                  {(c * 100).toFixed(0)}%
                </button>
              ))}
            </StrategyCard>
          </div>
        )}

      {!loading && !error && betType === 'spread' && spreadAll && spreadVal && (
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 mb-8">
          <StrategyCard result={spreadAll} />
          <StrategyCard result={spreadVal}>
            {SPREAD_EDGE_OPTIONS.map((e) => (
              <button
                key={e}
                onClick={() => setSpreadEdge(e)}
                className={`px-2 py-0.5 text-xs font-medium rounded-full transition-colors ${
                  spreadEdge === e
                    ? 'bg-accent-500 text-white'
                    : 'glass text-surface-500 dark:text-surface-400 hover:text-surface-700 dark:hover:text-surface-200'
                }`}
              >
                {e}
              </button>
            ))}
          </StrategyCard>
        </div>
      )}

      {!loading && !error && betType === 'overUnder' && totalAll && totalVal && (
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 mb-8">
          <StrategyCard result={totalAll} />
          <StrategyCard result={totalVal}>
            {TOTAL_EDGE_OPTIONS.map((e) => (
              <button
                key={e}
                onClick={() => setTotalEdge(e)}
                className={`px-2 py-0.5 text-xs font-medium rounded-full transition-colors ${
                  totalEdge === e
                    ? 'bg-accent-500 text-white'
                    : 'glass text-surface-500 dark:text-surface-400 hover:text-surface-700 dark:hover:text-surface-200'
                }`}
              >
                {e}
              </button>
            ))}
          </StrategyCard>
        </div>
      )}

      {!loading && !error && allStrategies.length > 0 && <StrategyChart results={allStrategies} />}

      <div className="glass rounded-xl p-6 mt-8">
        <h2 className="text-xs font-semibold uppercase tracking-wider text-surface-400 dark:text-surface-500 mb-4">
          How to Read This Page
        </h2>
        <div className="space-y-4 text-sm text-surface-600 dark:text-surface-400 leading-relaxed">
          <p>
            This page simulates flat{' '}
            <strong className="text-surface-800 dark:text-surface-200">1-unit bets</strong> using
            the In-House model's predictions against a selected bookmaker's odds. Every bet risks 1
            unit. P/L tracks cumulative units won or lost.
          </p>
          {betType === 'moneyline' && (
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <h3 className="font-semibold text-surface-800 dark:text-surface-200 mb-1">
                  In-House Winner
                </h3>
                <p>
                  Bets every game on whichever team the In-House model gives {'>'}50% chance to win.
                  This is the baseline — how does blindly following the model perform?
                </p>
              </div>
              <div>
                <h3 className="font-semibold text-surface-800 dark:text-surface-200 mb-1">
                  In-House Underdog
                </h3>
                <p>
                  The opposite of In-House Winner — only bets when the model picks a winner that the
                  bookmaker has as the underdog (implied probability {'<'}50%). These contrarian
                  bets pay out the most when they hit, since underdogs have longer odds.
                </p>
              </div>
              <div>
                <h3 className="font-semibold text-surface-800 dark:text-surface-200 mb-1">
                  Value Bets
                </h3>
                <p>
                  Only bets when the In-House model thinks a team's win probability is higher than
                  the bookmaker's implied probability by at least the{' '}
                  <strong className="text-surface-800 dark:text-surface-200">Value Edge</strong>{' '}
                  threshold.
                </p>
              </div>
              <div>
                <h3 className="font-semibold text-surface-800 dark:text-surface-200 mb-1">
                  Confidence
                </h3>
                <p>
                  Only bets when the In-House model's confidence in its pick exceeds the{' '}
                  <strong className="text-surface-800 dark:text-surface-200">Confidence</strong>{' '}
                  threshold. Skips coin-flip games.
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
                  Bets every game against the spread. If the model's predicted margin differs from
                  the bookmaker's line, bet the side the model favors. Pays out at the bookmaker's
                  spread price.
                </p>
              </div>
              <div>
                <h3 className="font-semibold text-surface-800 dark:text-surface-200 mb-1">
                  Spread Value
                </h3>
                <p>
                  Only bets when the model's predicted spread differs from the bookmaker's line by
                  at least the edge threshold (in goals).
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
                  bookmaker's line, bet over; if lower, bet under. Pays out at the bookmaker's o/u
                  price.
                </p>
              </div>
              <div>
                <h3 className="font-semibold text-surface-800 dark:text-surface-200 mb-1">
                  O/U Value
                </h3>
                <p>
                  Only bets when the model's predicted total differs from the bookmaker's line by at
                  least the edge threshold (in goals).
                </p>
              </div>
            </div>
          )}
          <div className="border-t border-surface-200 dark:border-white/[0.06] pt-4 text-xs text-surface-400 dark:text-surface-500">
            <p>
              <strong>Win Rate</strong> = wins / total bets. <strong>P/L</strong> = total units won
              minus total units lost. <strong>ROI</strong> = P/L / total bets as a percentage —
              positive ROI means profit per unit risked. The chart shows cumulative P/L over the
              season; above the dashed line is profit, below is loss.
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
