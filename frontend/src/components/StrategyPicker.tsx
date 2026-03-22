import { useStrategy } from '../contexts/StrategyContext';
import { STRATEGY_OPTIONS, type StrategyType } from '../utils/bettingStrategies';

export function StrategyPicker() {
  const { strategy, setStrategy } = useStrategy();
  const current = STRATEGY_OPTIONS.find((o) => o.type === strategy.type);

  return (
    <div className="flex flex-wrap items-center gap-2">
      <select
        value={strategy.type}
        onChange={(e) => {
          const type = e.target.value as StrategyType;
          const opt = STRATEGY_OPTIONS.find((o) => o.type === type);
          const threshold = opt?.thresholds?.[Math.floor((opt.thresholds.length - 1) / 2)] ?? 0;
          setStrategy({ type, threshold });
        }}
        className="glass px-3 py-1.5 rounded-lg text-xs font-medium bg-transparent border-0 cursor-pointer text-surface-700 dark:text-surface-300"
      >
        {STRATEGY_OPTIONS.map((opt) => (
          <option key={opt.type} value={opt.type}>
            {opt.label}
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
              {`${(t * 100).toFixed(0)}%`}
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
