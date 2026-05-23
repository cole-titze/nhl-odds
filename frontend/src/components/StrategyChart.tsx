import { useState, useMemo, useEffect } from 'react';
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  Tooltip,
  ResponsiveContainer,
  ReferenceLine,
} from 'recharts';
import { formatShortDate } from '../utils/dates';
import type { StrategyResult } from '../utils/bettingStrategies';
import { getButtonClasses, getTextClass } from '../utils/colorClass';

const STRATEGY_COLORS: Record<string, string> = {
  'In-House Winner': '#3b82f6',
  'Value Bets': '#22c55e',
  'In-House Underdog': '#f97316',
  Confidence: '#a855f7',
  'Spread Bet': '#3b82f6',
  'Spread Value': '#22c55e',
  'O/U Bet': '#3b82f6',
  'O/U Value': '#22c55e',
};

interface Props {
  results: StrategyResult[];
}

export function StrategyChart({ results }: Props) {
  const strategyNames = useMemo(() => results.map((r) => r.name), [results]);

  const [enabled, setEnabled] = useState<Set<string>>(() => new Set(strategyNames));

  // Reset enabled set when strategy names change (e.g. switching from moneyline to spread)
  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect
    setEnabled(new Set(strategyNames));
  }, [strategyNames]);

  const toggle = (name: string) => {
    setEnabled((prev) => {
      const next = new Set(prev);
      if (next.has(name)) next.delete(name);
      else next.add(name);
      return next;
    });
  };

  const chartData = useMemo(() => {
    const allDates = new Map<string, Record<string, number | string>>();

    for (const result of results) {
      for (const bet of result.bets) {
        const dateKey = bet.gameDate;
        if (!allDates.has(dateKey)) {
          allDates.set(dateKey, {
            date: formatShortDate(dateKey),
            sortKey: dateKey,
          });
        }
        allDates.get(dateKey)![result.name] = bet.cumulativePL;
      }
    }

    return Array.from(allDates.values()).sort((a, b) =>
      (a.sortKey as string).localeCompare(b.sortKey as string),
    );
  }, [results]);

  if (chartData.length <= 1) return null;

  return (
    <div className="glass rounded-xl p-5">
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-xs font-semibold uppercase tracking-wider text-surface-400 dark:text-surface-500">
          Cumulative P/L (units)
        </h2>
      </div>
      <div className="flex flex-wrap gap-2 mb-4">
        {strategyNames.map((name) => {
          const color = STRATEGY_COLORS[name] ?? '#737373';
          const active = enabled.has(name);
          return (
            <button
              key={name}
              onClick={() => toggle(name)}
              className={`px-2.5 py-1 rounded-full text-xs font-mono font-medium transition-all ${getButtonClasses(color, active)}`}
            >
              {name}
            </button>
          );
        })}
      </div>
      <ResponsiveContainer width="100%" height={300}>
        <LineChart data={chartData}>
          <XAxis
            dataKey="date"
            tick={{ fontSize: 11, fontFamily: 'JetBrains Mono' }}
            interval="preserveStartEnd"
            stroke="#525252"
            tickLine={false}
            axisLine={false}
          />
          <YAxis
            tick={{ fontSize: 11, fontFamily: 'JetBrains Mono' }}
            domain={['auto', 'auto']}
            stroke="#525252"
            tickLine={false}
            axisLine={false}
          />
          <ReferenceLine y={0} stroke="#525252" strokeDasharray="4 4" />
          <Tooltip
            content={({ active, payload, label }) => {
              if (!active || !payload?.length) return null;
              const d = payload[0].payload as Record<string, unknown>;
              return (
                <div className="bg-[rgba(10,10,10,0.9)] border border-white/8 rounded-lg font-mono text-xs text-white backdrop-blur-md py-2 px-3">
                  <div>{label}</div>
                  {strategyNames
                    .filter((name) => enabled.has(name) && d[name] != null)
                    .map((name) => (
                      <div key={name} className={getTextClass(STRATEGY_COLORS[name] ?? '#737373')}>
                        {name}: {(d[name] as number).toFixed(2)}u
                      </div>
                    ))}
                </div>
              );
            }}
          />
          {strategyNames
            .filter((name) => enabled.has(name))
            .map((name) => {
              const color = STRATEGY_COLORS[name] ?? '#737373';
              return (
                <Line
                  key={name}
                  type="monotone"
                  dataKey={name}
                  stroke={color}
                  strokeWidth={2.5}
                  dot={false}
                  activeDot={{
                    r: 4,
                    fill: color,
                    stroke: '#141418',
                    strokeWidth: 2,
                  }}
                  connectNulls
                />
              );
            })}
        </LineChart>
      </ResponsiveContainer>
    </div>
  );
}
