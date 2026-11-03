import { useMemo } from 'react';
import { LineChart, Line, XAxis, YAxis, Tooltip, ResponsiveContainer } from 'recharts';
import { useFetch } from '../hooks/useFetch';
import { useHydrated } from '../hooks/useHydrated';
import { useIsDarkTheme } from '../hooks/useRootTheme';
import { useDisplayTimeZone } from '../hooks/useDisplayTimeZone';
import { getOddsMovement } from '../api/oddsMovement';
import { dashFor, readableLineColor } from '../utils/chartSeries';
import { LineKey } from './LineKey';
import { ChartDataTable } from './ChartDataTable';
import { Skeleton } from './Skeleton';
import { LoadingStatus } from './A11y';
import type { GameOddsVM, OddsMovementVM } from '../types';

const HEADING =
  'text-xs font-mono font-bold tracking-widest uppercase text-surface-600 dark:text-surface-400';
const AXIS_TICK = { fontSize: 11, fontFamily: 'JetBrains Mono Variable', fill: 'currentColor' };
const KIND_COLORS: Record<string, string> = {
  model: '#3b82f6',
  average: '#f97316',
  kalshi: '#14b8a6',
};
const PARTNER_COLORS = ['#22c55e', '#a855f7', '#e44d2e'];

const pct = (v: number) => `${(v * 100).toFixed(1)}%`;

/** The home team's win chance before puck drop: the model next to each market's prices */
export function OddsMovementChart({ game }: { game: GameOddsVM }) {
  const { data, loading } = useFetch(() => getOddsMovement(game.id), [game.id]);

  if (loading)
    return (
      <div className="glass rounded-xl p-6 space-y-3">
        <LoadingStatus label="Loading odds movement…" />
        <Skeleton className="h-4 w-40" />
        <Skeleton className="h-64 w-full" />
      </div>
    );
  // Not worth an error box: the rest of the page already shows the odds
  if (!data || data.series.length === 0) return null;
  return <Chart game={game} movement={data} />;
}

function Chart({ game, movement }: { game: GameOddsVM; movement: OddsMovementVM }) {
  const hydrated = useHydrated();
  const dark = useIsDarkTheme();
  const { locales, timeZone } = useDisplayTimeZone();
  const home = game.homeTeam ? `${game.homeTeam.locationName} ${game.homeTeam.teamName}` : 'Home';

  const styles = useMemo(() => {
    let partner = 0;
    return movement.series.map((s, i) => {
      const base = s.kind === 'partner' ? PARTNER_COLORS[partner++ % 3] : KIND_COLORS[s.kind];
      return { name: s.name, base, color: readableLineColor(base, dark), dash: dashFor(i) };
    });
  }, [movement, dark]);

  // One row per time any source changed, each source carried forward from its first price (a step
  // chart: a price holds until the next one), ending at puck drop
  const rows = useMemo(() => {
    const start = Date.parse(movement.startUTC);
    const times = [
      ...new Set([
        ...movement.series.flatMap((s) => s.points.map((p) => Date.parse(p.timeUTC))),
        start,
      ]),
    ].sort((a, b) => a - b);
    return times.map((t) => {
      const row: Record<string, number | undefined> = { t };
      for (const s of movement.series) {
        const last = s.points.filter((p) => Date.parse(p.timeUTC) <= t).at(-1);
        row[s.name] = last?.homeProbability;
      }
      return row;
    });
  }, [movement]);

  const spansDays = rows.length > 1 && rows[rows.length - 1].t! - rows[0].t! > 20 * 3600 * 1000;
  const ticks = hourTicks(rows[0].t!, rows[rows.length - 1].t!);
  const formatTick = (t: number) =>
    new Date(t).toLocaleString(locales, {
      ...(spansDays ? { month: 'short', day: 'numeric' } : {}),
      hour: 'numeric',
      timeZone,
    });
  const formatTime = (t: number) =>
    new Date(t).toLocaleString(locales, {
      ...(spansDays ? { month: 'short', day: 'numeric' } : {}),
      hour: 'numeric',
      minute: '2-digit',
      timeZone,
    });

  const move = movement.move;
  return (
    <section className="glass rounded-xl p-6" aria-labelledby="odds-movement-heading">
      <h2 id="odds-movement-heading" className={HEADING}>
        Odds Movement
      </h2>
      <p className="text-xs text-surface-600 dark:text-surface-400 mt-1 mb-3">
        {home} win chance from each source until puck drop, with the bookmaker&apos;s margin
        removed.
      </p>
      {move ? (
        <p className="text-sm text-surface-700 dark:text-surface-300 mb-4">
          Sportsbooks went from {pct(move.open)} in the morning to {pct(move.close)} at the close (
          {move.close >= move.open ? '+' : '−'}
          {Math.abs((move.close - move.open) * 100).toFixed(1)} pts)
          {move.towardModel != null && move.model != null
            ? Math.abs(move.towardModel) < 0.0005
              ? `, not moving relative to the model's ${pct(move.model)}.`
              : `, ${move.towardModel > 0 ? 'toward' : 'away from'} the model's ${pct(move.model)}.`
            : '.'}
        </p>
      ) : (
        !game.hasBeenPlayed && (
          <p className="text-sm text-surface-700 dark:text-surface-300 mb-4">
            Closing prices are saved just before puck drop.
          </p>
        )
      )}
      <div className="flex flex-wrap gap-x-4 gap-y-1 text-xs font-mono mb-2" aria-hidden="true">
        {styles.map((s) => (
          <span key={s.name} className="inline-flex items-center gap-1.5">
            <LineKey color={s.color} dash={s.dash} />
            {s.name}
          </span>
        ))}
      </div>
      <div className="text-surface-600 dark:text-surface-400">
        {/* Recharts sizes itself with inline styles, which the CSP blocks in server HTML */}
        {!hydrated ? (
          <div className="h-[260px]" />
        ) : (
          <ResponsiveContainer width="100%" height={260}>
            <LineChart data={rows} margin={{ top: 8, right: 16, bottom: 8, left: 0 }}>
              <XAxis
                type="number"
                dataKey="t"
                scale="time"
                domain={['dataMin', 'dataMax']}
                ticks={ticks}
                tickFormatter={formatTick}
                tick={AXIS_TICK}
                minTickGap={16}
                stroke="#525252"
                tickLine={false}
                axisLine={false}
              />
              <YAxis
                domain={['auto', 'auto']}
                tickFormatter={(v: number) => `${Math.round(v * 100)}%`}
                tick={AXIS_TICK}
                width={44}
                stroke="#525252"
                tickLine={false}
                axisLine={false}
              />
              <Tooltip
                content={({ active, payload }) => {
                  if (!active || !payload?.length) return null;
                  const row = payload[0].payload as Record<string, number | undefined>;
                  return (
                    <div className="bg-[rgba(10,10,10,0.9)] border border-white/8 rounded-lg font-mono text-xs text-white backdrop-blur-md py-2 px-3">
                      <div>{formatTime(row.t!)}</div>
                      {styles
                        .filter((s) => row[s.name] != null)
                        .map((s) => (
                          <div key={s.name} className="flex items-center gap-1.5">
                            <LineKey color={readableLineColor(s.base, true)} dash={s.dash} />
                            {s.name}: {pct(row[s.name]!)}
                          </div>
                        ))}
                    </div>
                  );
                }}
              />
              {styles.map((s) => (
                <Line
                  key={s.name}
                  type="stepAfter"
                  dataKey={s.name}
                  stroke={s.color}
                  strokeDasharray={s.dash || undefined}
                  // The draw-in animation overwrites strokeDasharray
                  isAnimationActive={false}
                  strokeWidth={2.5}
                  dot={false}
                  activeDot={{ r: 4, fill: s.color, stroke: '#141418', strokeWidth: 2 }}
                />
              ))}
            </LineChart>
          </ResponsiveContainer>
        )}
      </div>
      <ChartDataTable
        caption={`${home} win chance by time and source`}
        rowHeader="Time"
        columns={styles.map((s) => s.name)}
        rows={rows.map((r) => ({
          key: String(r.t),
          label: formatTime(r.t!),
          values: Object.fromEntries(styles.map((s) => [s.name, r[s.name]])),
        }))}
        format={(v) => pct(v)}
      />
    </section>
  );
}

const HOUR = 3600 * 1000;

/** Whole-hour ticks, at most about five, none so close to an edge that its label is cut off */
function hourTicks(from: number, to: number): number[] {
  const span = to - from;
  const step = [1, 2, 3, 4, 6, 12, 24].find((h) => span / (h * HOUR) <= 5) ?? 48;
  const ticks: number[] = [];
  for (let t = Math.ceil(from / (step * HOUR)) * step * HOUR; t <= to; t += step * HOUR)
    if (t - from > span * 0.06 && to - t > span * 0.06) ticks.push(t);
  return ticks;
}
