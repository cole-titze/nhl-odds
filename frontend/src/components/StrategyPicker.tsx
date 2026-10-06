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
    <div className="flex flex-col items-center sm:items-start gap-1.5 min-w-0 max-w-full">
      <div className="flex items-center gap-2 h-7 min-w-0 max-w-full">
        <select
          value={current ? strategy.type : filtered[0]?.type}
          onChange={(e) => {
            const type = e.target.value as StrategyType;
            const opt = filtered.find((o) => o.type === type);
            const threshold = opt?.thresholds?.[Math.floor((opt.thresholds.length - 1) / 2)] ?? 0;
            setStrategy({ type, threshold });
          }}
          aria-label="Betting strategy"
          className="shrink-0 glass px-3 py-1.5 rounded-lg text-xs font-medium bg-transparent border-0 cursor-pointer text-surface-700 dark:text-surface-300"
        >
          {filtered.map((opt) => (
            <option key={opt.type} value={opt.type}>
              {renameLabel ? renameLabel(opt.label) : opt.label}
              {best?.strategyType === opt.type ? ' ★' : ''}
            </option>
          ))}
        </select>
        {best &&
          (() => {
            const title = `Best backtested strategy: ${best.wins}/${best.bets} bets won, ${formatSeasonLabel(best.firstSeason)} through ${formatSeasonLabel(best.lastSeason)}, DraftKings + Kalshi, profitable every season`;
            const onSuggested =
              strategy.type === best.strategyType && strategy.threshold === best.threshold;
            if (onSuggested) {
              return (
                <span
                  className="min-w-0 truncate px-2 py-1 rounded-full bg-accent-500/10 text-accent-600 dark:text-accent-400 text-[11px] font-semibold"
                  title={title}
                >
                  <span aria-hidden="true">★ </span>Suggested · +{best.roi.toFixed(1)}% ROI
                  <span className="sr-only">. {title}</span>
                </span>
              );
            }
            const label =
              STRATEGY_OPTIONS.find((o) => o.type === best.strategyType)?.label ??
              best.strategyType;
            return (
              <button
                type="button"
                onClick={() => setStrategy({ type: best.strategyType, threshold: best.threshold })}
                className="min-w-0 truncate glass px-2 py-1 rounded-full text-[11px] font-medium text-surface-500 dark:text-surface-400 hover:text-accent-600 dark:hover:text-accent-400 transition-colors"
                title={title}
              >
                <span aria-hidden="true">★ </span>Use suggested:{' '}
                {renameLabel ? renameLabel(label) : label}
              </button>
            );
          })()}
      </div>
      {/* Row reserved even without thresholds so the picker (and the sticky filter bar
          holding it) keeps one height across strategies */}
      {current?.thresholds ? (
        <div className="flex gap-1 h-[26px]" role="group" aria-label="Edge threshold">
          {current.thresholds.map((t) => (
            <button
              key={t}
              type="button"
              aria-pressed={strategy.threshold === t}
              onClick={() => setStrategy({ ...strategy, threshold: t })}
              title={
                best?.strategyType === strategy.type && best.threshold === t
                  ? 'Best historical threshold'
                  : undefined
              }
              className={`px-2 py-1 text-[11px] font-mono font-medium rounded-md transition-colors ${
                strategy.threshold === t
                  ? 'bg-accent-600 text-on-accent'
                  : 'glass text-surface-500 dark:text-surface-400 hover:text-surface-700 dark:hover:text-surface-200'
              }`}
            >
              {formatThreshold(t)}
              {best?.strategyType === strategy.type && best.threshold === t && (
                <>
                  <span aria-hidden="true"> ★</span>
                  <span className="sr-only"> (best historical threshold)</span>
                </>
              )}
            </button>
          ))}
        </div>
      ) : (
        <div className="h-[26px]" aria-hidden="true" />
      )}
    </div>
  );
}
