import { useState, useMemo } from 'react';
import { LineChart, Line, XAxis, YAxis, Tooltip, ResponsiveContainer } from 'recharts';
import { formatShortDate } from '../utils/dates';
import { calculateLogLoss } from '../utils/predictions';
import type { GameOddsVM } from '../types';
import { getButtonClasses, getTextClass } from '../utils/colorClass';

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

    return sorted.map((g) => {
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

      return point;
    });
  }, [playedGames, bookmakerNames]);

  if (chartData.length <= 1) return null;

  return (
    <div className="glass rounded-xl p-5 mb-8">
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-xs font-semibold uppercase tracking-wider text-surface-400 dark:text-surface-500">
          Cumulative Avg Log Loss
        </h2>
      </div>
      <div className="flex flex-wrap gap-2 mb-4">
        {allChips.map((name, i) => {
          const color = getColor(name, i);
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
      <ResponsiveContainer width="100%" height={250}>
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
                      <div
                        key={name}
                        className={getTextClass(getColor(name, allChips.indexOf(name)))}
                      >
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
              const color = getColor(name, allChips.indexOf(name));
              return (
                <Line
                  key={name}
                  type="monotone"
                  dataKey={name}
                  stroke={color}
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
  );
}
