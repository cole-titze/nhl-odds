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
import { getChipClasses } from '../utils/colorClass';
import { dashFor, readableLineColor } from '../utils/chartSeries';
import { useIsDarkTheme } from '../hooks/useRootTheme';
import { LineKey } from './LineKey';
import { ChartDataTable } from './ChartDataTable';

const STRATEGY_COLORS: Record<string, string> = {
  'In-House Winner': '#3b82f6',
  'Value Bets': '#22c55e',
  'In-House Underdog': '#f97316',
  'Underdog Value': '#e44d2e',
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
  const dark = useIsDarkTheme();
  const baseColor = (name: string) => STRATEGY_COLORS[name] ?? '#737373';
  const lineColor = (name: string) => readableLineColor(baseColor(name), dark);
  const lineDash = (name: string) => dashFor(strategyNames.indexOf(name));

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

  // The SVG chart isn't readable by screen readers, so summarize the final P/L
  const summary = `Final cumulative profit/loss: ${results
    .filter((r) => enabled.has(r.name) && r.bets.length > 0)
    .map((r) => `${r.name} ${r.bets[r.bets.length - 1].cumulativePL.toFixed(2)} units`)
    .join(', ')}.`;

  return (
    <div className="glass rounded-xl p-5">
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-xs font-semibold uppercase tracking-wider text-surface-500 dark:text-surface-400">
          Cumulative P/L (units)
        </h2>
      </div>
      <div className="flex flex-wrap gap-2 mb-4" role="group" aria-label="Lines shown">
        {strategyNames.map((name) => {
          const color = STRATEGY_COLORS[name] ?? '#737373';
          const active = enabled.has(name);
          return (
            <button
              key={name}
              type="button"
              aria-pressed={active}
              onClick={() => toggle(name)}
              className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-mono font-medium transition-all ${getChipClasses(color, active)}`}
            >
              <LineKey color={lineColor(name)} dash={lineDash(name)} active={active} />
              <span className={active ? '' : 'line-through'}>{name}</span>
            </button>
          );
        })}
      </div>
      <p className="sr-only">{summary}</p>
      <div className="text-surface-600 dark:text-surface-400">
        <ResponsiveContainer width="100%" height={300}>
          <LineChart data={chartData}>
            <XAxis
              dataKey="date"
              tick={{ fontSize: 11, fontFamily: 'JetBrains Mono', fill: 'currentColor' }}
              interval="preserveStartEnd"
              stroke="#525252"
              tickLine={false}
              axisLine={false}
            />
            <YAxis
              tick={{ fontSize: 11, fontFamily: 'JetBrains Mono', fill: 'currentColor' }}
              domain={['auto', 'auto']}
              stroke="#525252"
              tickLine={false}
              axisLine={false}
            />
            <ReferenceLine y={0} stroke="currentColor" strokeDasharray="4 4" />
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
                        <div key={name} className="flex items-center gap-1.5">
                          <LineKey
                            color={readableLineColor(baseColor(name), true)}
                            dash={lineDash(name)}
                          />
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
                const color = lineColor(name);
                return (
                  <Line
                    key={name}
                    type="monotone"
                    dataKey={name}
                    stroke={color}
                    strokeDasharray={lineDash(name) || undefined}
                    // Recharts' draw-in animation overwrites strokeDasharray (and
                    // blanks it under prefers-reduced-motion), erasing the patterns
                    isAnimationActive={false}
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
      <ChartDataTable
        caption="Cumulative profit/loss in units by date"
        rowHeader="Date"
        columns={strategyNames.filter((name) => enabled.has(name))}
        rows={chartData.map((point) => ({
          key: point.sortKey as string,
          label: point.date as string,
          values: Object.fromEntries(
            strategyNames.map((name) => [name, point[name] as number | undefined]),
          ),
        }))}
        format={(v) => `${v >= 0 ? '+' : ''}${v.toFixed(2)}u`}
      />
    </div>
  );
}
