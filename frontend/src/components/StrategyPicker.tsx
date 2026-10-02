import { useEffect } from 'react';
import { useStrategy } from '../contexts/StrategyContext';
import {
  STRATEGY_OPTIONS,
  type BetTypeCategory,
  type StrategyType,
} from '../utils/bettingStrategies';
import { formatSeasonLabel } from '../utils/season';

interface Props {
  betType?: BetTypeCategory;
  renameLabel?: (label: string) => string;
}

export function StrategyPicker({ betType, renameLabel }: Props) {
  const { strategy, setStrategy, suggested } = useStrategy();
  const filtered = betType
    ? STRATEGY_OPTIONS.filter((o) => o.betType === betType)
    : STRATEGY_OPTIONS;
  const current = filtered.find((o) => o.type === strategy.type);
  const best = suggested.find((b) => b.betType === (betType ?? current?.betType));

  // When betType changes, switch to the historically best strategy of that type,
  // falling back to the first one
  useEffect(() => {
    if (betType && !filtered.some((o) => o.type === strategy.type)) {
      if (best) {
        setStrategy({ type: best.strategyType, threshold: best.threshold });
        return;
      }
      const first = filtered[0];
      const threshold = first?.thresholds?.[Math.floor((first.thresholds.length - 1) / 2)] ?? 0;
      setStrategy({ type: first.type, threshold });
    }
  }, [betType, filtered, best, strategy.type, setStrategy]);

  const formatThreshold = (t: number) =>
    current?.thresholdFormat === 'goals' ? `${t}` : `${(t * 100).toFixed(0)}%`;

  return (
    <div className="flex flex-wrap items-center gap-2">
      <select
        value={current ? strategy.type : filtered[0]?.type}
        onChange={(e) => {
          const type = e.target.value as StrategyType;
          const opt = filtered.find((o) => o.type === type);
          const threshold = opt?.thresholds?.[Math.floor((opt.thresholds.length - 1) / 2)] ?? 0;
          setStrategy({ type, threshold });
        }}
        className="glass px-3 py-1.5 rounded-lg text-xs font-medium bg-transparent border-0 cursor-pointer text-surface-700 dark:text-surface-300"
      >
        {filtered.map((opt) => (
          <option key={opt.type} value={opt.type}>
            {renameLabel ? renameLabel(opt.label) : opt.label}
            {best?.strategyType === opt.type ? ' ★' : ''}
          </option>
        ))}
      </select>
      {current?.thresholds && (
        <div className="flex gap-1">
          {current.thresholds.map((t) => (
            <button
              key={t}
              onClick={() => setStrategy({ ...strategy, threshold: t })}
              title={
                best?.strategyType === strategy.type && best.threshold === t
                  ? 'Best historical threshold'
                  : undefined
              }
              className={`px-2 py-1 text-[11px] font-mono font-medium rounded-md transition-colors ${
                strategy.threshold === t
                  ? 'bg-accent-500 text-white'
                  : 'glass text-surface-500 dark:text-surface-400 hover:text-surface-700 dark:hover:text-surface-200'
              }`}
            >
              {formatThreshold(t)}
              {best?.strategyType === strategy.type && best.threshold === t ? ' ★' : ''}
            </button>
          ))}
        </div>
      )}
      {best &&
        (() => {
          const title = `Best backtested strategy: ${best.wins}/${best.bets} bets won, ${formatSeasonLabel(best.firstSeason)} through ${formatSeasonLabel(best.lastSeason)}, DraftKings + Kalshi`;
          const onSuggested =
            strategy.type === best.strategyType && strategy.threshold === best.threshold;
          if (onSuggested) {
            return (
              <span
                className="px-2 py-1 rounded-full bg-accent-500/10 text-accent-600 dark:text-accent-400 text-[11px] font-semibold"
                title={title}
              >
                ★ Suggested · +{best.roi.toFixed(1)}% ROI
              </span>
            );
          }
          const label =
            STRATEGY_OPTIONS.find((o) => o.type === best.strategyType)?.label ?? best.strategyType;
          return (
            <button
              type="button"
              onClick={() => setStrategy({ type: best.strategyType, threshold: best.threshold })}
              className="glass px-2 py-1 rounded-full text-[11px] font-medium text-surface-500 dark:text-surface-400 hover:text-accent-600 dark:hover:text-accent-400 transition-colors"
              title={title}
            >
              ★ Use suggested: {renameLabel ? renameLabel(label) : label}
            </button>
          );
        })()}
    </div>
  );
}
