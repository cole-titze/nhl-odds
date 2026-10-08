import { useState, useMemo } from 'react';
import { LineChart, Line, XAxis, YAxis, Tooltip, ResponsiveContainer } from 'recharts';
import { formatShortDate } from '../utils/dates';
import { calculateLogLoss } from '../utils/predictions';
import type { GameOddsVM } from '../types';
import { getChipClasses } from '../utils/colorClass';
import { dashFor, readableLineColor } from '../utils/chartSeries';
import { useIsDarkTheme } from '../hooks/useRootTheme';
import { LineKey } from './LineKey';
import { ChartDataTable, type ChartTableRow } from './ChartDataTable';

const HOMEGROWN = 'In-House Model';

const LINE_COLORS: Record<string, string> = {
  [HOMEGROWN]: '#3b82f6',
  DraftKings: '#53d337',
  FanDuel: '#1493ff',
  BetMGM: '#c4a44a',
  Bovada: '#e53e3e',
  BetRivers: '#1e90ff',
  'William Hill (US)': '#1b3668',
  PointsBet: '#e44d2e',
  Caesars: '#0a4d3c',
  Unibet: '#14805e',
  BetOnline: '#8b0000',
  Kalshi: '#7c3aed',
};

const FALLBACK_COLORS = [
  '#e879f9',
  '#fb923c',
  '#a78bfa',
  '#facc15',
  '#34d399',
  '#f87171',
  '#60a5fa',
  '#c084fc',
  '#4ade80',
  '#fbbf24',
];

function getColor(name: string, index: number): string {
  return LINE_COLORS[name] ?? FALLBACK_COLORS[index % FALLBACK_COLORS.length];
}

const DEFAULT_ENABLED = new Set([HOMEGROWN, 'DraftKings', 'Kalshi']);

interface Props {
  games: GameOddsVM[];
  deduplicateById?: boolean;
}

export function LogLossChart({ games, deduplicateById }: Props) {
  const playedGames = useMemo(() => {
    const filtered = games.filter((g) => g.hasBeenPlayed && g.logLoss != null && g.logLoss > 0);
    if (!deduplicateById) return filtered;
    const seen = new Set<number>();
    return filtered.filter((g) => {
      if (seen.has(g.id)) return false;
      seen.add(g.id);
      return true;
    });
  }, [games, deduplicateById]);

  const bookmakerNames = useMemo(() => {
    const names = new Set<string>();
    for (const g of playedGames) {
      for (const b of g.bookmakerOdds ?? []) {
        if (b.homeOdds > 0 && b.awayOdds > 0) names.add(b.bookmakerName);
      }
    }
    return Array.from(names).sort();
  }, [playedGames]);

  const allChips = useMemo(() => [HOMEGROWN, ...bookmakerNames], [bookmakerNames]);

  const [enabled, setEnabled] = useState<Set<string>>(DEFAULT_ENABLED);
  const dark = useIsDarkTheme();
  // Each series keeps its slot (color + dash pattern) whether or not it's shown
  const lineColor = (name: string) =>
    readableLineColor(getColor(name, allChips.indexOf(name)), dark);
  const lineDash = (name: string) => dashFor(allChips.indexOf(name));

  const toggle = (name: string) => {
    setEnabled((prev) => {
      const next = new Set(prev);
      if (next.has(name)) next.delete(name);
      else next.add(name);
      return next;
    });
  };

  const chartData = useMemo(() => {
    const sorted = [...playedGames].sort(
      (a, b) => new Date(a.gameDate).getTime() - new Date(b.gameDate).getTime(),
    );

    let cumLogLoss = 0;
    let count = 0;
    const cumByBookmaker: Record<string, { sum: number; count: number }> = {};

    const points: Record<string, string | number | undefined>[] = [];
    for (const g of sorted) {
      cumLogLoss += g.logLoss ?? 0;
      count++;

      const point: Record<string, string | number | undefined> = {
        date: formatShortDate(g.gameDate),
        [HOMEGROWN]: +(cumLogLoss / count).toFixed(4),
        games: count,
      };

      for (const name of bookmakerNames) {
        const b = g.bookmakerOdds?.find((bk) => bk.bookmakerName === name);
        if (b && b.homeOdds > 0 && b.awayOdds > 0) {
          const loss = calculateLogLoss(b.homeOdds, b.awayOdds, g.winner);
          if (!cumByBookmaker[name]) cumByBookmaker[name] = { sum: 0, count: 0 };
          cumByBookmaker[name].sum += loss;
          cumByBookmaker[name].count++;
        }
        if (cumByBookmaker[name]?.count > 0) {
          point[name] = +(cumByBookmaker[name].sum / cumByBookmaker[name].count).toFixed(4);
        }
      }

      points.push(point);
    }
    return points;
  }, [playedGames, bookmakerNames]);

  // One row per day (the chart plots every game; a season has ~1,300)
  const shown = allChips.filter((name) => enabled.has(name));
  const tableRows = useMemo(() => {
    const byDate = new Map<string, ChartTableRow>();
    for (const point of chartData) {
      const label = point.date as string;
      const values: ChartTableRow['values'] = { Games: point.games as number };
      for (const name of allChips) values[name] = point[name] as number | undefined;
      byDate.set(label, { key: label, label, values });
    }
    return [...byDate.values()];
  }, [chartData, allChips]);

  if (chartData.length <= 1) return null;

  // The SVG chart isn't readable by screen readers, so summarize where each line ends up
  const last = chartData[chartData.length - 1];
  const summary = `After ${last.games} games: ${allChips
    .filter((name) => enabled.has(name) && last[name] != null)
    .map((name) => `${name} ${last[name]}`)
    .join(', ')}. Lower is better.`;

  return (
    <div className="glass rounded-xl p-5 mb-8">
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-xs font-semibold uppercase tracking-wider text-surface-500 dark:text-surface-400">
          Cumulative Avg Log Loss
        </h2>
      </div>
      <div className="flex flex-wrap gap-2 mb-4" role="group" aria-label="Lines shown">
        {allChips.map((name, i) => {
          const color = getColor(name, i);
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
        <ResponsiveContainer width="100%" height={250}>
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
            <Tooltip
              content={({ active, payload, label }) => {
                if (!active || !payload?.length) return null;
                const d = payload[0].payload as Record<string, unknown>;
                return (
                  <div className="bg-[rgba(10,10,10,0.9)] border border-white/8 rounded-lg font-mono text-xs text-white backdrop-blur-md py-2 px-3">
                    <div>{label}</div>
                    {allChips
                      .filter((name) => enabled.has(name) && d[name] != null)
                      .map((name) => (
                        <div key={name} className="flex items-center gap-1.5">
                          <LineKey
                            color={readableLineColor(getColor(name, allChips.indexOf(name)), true)}
                            dash={lineDash(name)}
                          />
                          {name}: {d[name] as number}
                        </div>
                      ))}
                    <div>games: {d.games as number}</div>
                  </div>
                );
              }}
            />
            {allChips
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
                    activeDot={{ r: 4, fill: color, stroke: '#141418', strokeWidth: 2 }}
                    connectNulls
                  />
                );
              })}
          </LineChart>
        </ResponsiveContainer>
      </div>
      <ChartDataTable
        caption="Cumulative average log loss by date (lower is better)"
        rowHeader="Date"
        columns={['Games', ...shown]}
        rows={tableRows}
        format={(v, column) => (column === 'Games' ? String(v) : v.toFixed(4))}
      />
    </div>
  );
}
