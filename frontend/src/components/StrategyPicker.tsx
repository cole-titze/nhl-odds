import { useEffect } from 'react';
import { useStrategy } from '../contexts/StrategyContext';
import {
  STRATEGY_OPTIONS,
  type BetTypeCategory,
  type StrategyType,
} from '../utils/bettingStrategies';

interface Props {
  betType?: BetTypeCategory;
  renameLabel?: (label: string) => string;
}

export function StrategyPicker({ betType, renameLabel }: Props) {
  const { strategy, setStrategy } = useStrategy();
  const filtered = betType
    ? STRATEGY_OPTIONS.filter((o) => o.betType === betType)
    : STRATEGY_OPTIONS;
  const current = filtered.find((o) => o.type === strategy.type);

  // When betType changes, switch to the first strategy of that type
  useEffect(() => {
    if (betType && !filtered.some((o) => o.type === strategy.type)) {
      const first = filtered[0];
      const threshold = first?.thresholds?.[Math.floor((first.thresholds.length - 1) / 2)] ?? 0;
      setStrategy({ type: first.type, threshold });
    }
  }, [betType, filtered, strategy.type, setStrategy]);

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
          </option>
        ))}
      </select>
      {current?.thresholds && (
        <div className="flex gap-1">
          {current.thresholds.map((t) => (
            <button
              key={t}
              onClick={() => setStrategy({ ...strategy, threshold: t })}
              className={`px-2 py-1 text-[11px] font-mono font-medium rounded-md transition-colors ${
                strategy.threshold === t
                  ? 'bg-accent-500 text-white'
                  : 'glass text-surface-500 dark:text-surface-400 hover:text-surface-700 dark:hover:text-surface-200'
              }`}
            >
              {current.thresholdFormat === 'goals' ? t : `${(t * 100).toFixed(0)}%`}
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
