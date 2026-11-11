// The Track Record page's Recharts charts. The page loads this module lazily (inside LazyChart), so
// the chart library downloads and draws only when a chart scrolls near the screen.
import {
  CartesianGrid,
  ComposedChart,
  Line,
  LineChart,
  ReferenceLine,
  ResponsiveContainer,
  Scatter,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts';
import { LineKey } from './LineKey';
import { readableLineColor } from '../utils/chartSeries';
import { COIN_FLIP, MARKET_COLOR, MODEL, MODEL_COLOR, ll, pct } from '../utils/trackRecord';
import type { CalibrationBinVM } from '../types';

const AXIS_TICK = { fontSize: 11, fontFamily: 'JetBrains Mono Variable', fill: 'currentColor' };
const AXIS_LABEL = { fontSize: 11, fill: 'currentColor' };
const TOOLTIP_CLASS =
  'bg-[rgba(10,10,10,0.9)] border border-white/8 rounded-lg font-mono text-xs text-white backdrop-blur-md py-2 px-3';

export type CalibrationRow = CalibrationBinVM & { label: string };
export type LogLossRow = { key: string; date: string; games: number } & Record<
  string,
  number | string | undefined
>;
export type LogLossSeries = { name: string; color: string; dash: string };

export function CalibrationPlot({ data, color }: { data: CalibrationRow[]; color: string }) {
  return (
    <ResponsiveContainer width="100%" height={300}>
      <ComposedChart data={data} margin={{ top: 8, right: 8, bottom: 20, left: 8 }}>
        <CartesianGrid stroke="currentColor" strokeOpacity={0.1} />
        <XAxis
          type="number"
          dataKey="meanPredicted"
          domain={[0, 1]}
          ticks={[0, 0.2, 0.4, 0.6, 0.8, 1]}
          tickFormatter={(v: number) => pct(v, 0)}
          tick={AXIS_TICK}
          stroke="#525252"
          tickLine={false}
          axisLine={false}
          label={{
            ...AXIS_LABEL,
            value: 'Model: home win chance',
            position: 'bottom',
            offset: 4,
          }}
        />
        <YAxis
          type="number"
          domain={[0, 1]}
          ticks={[0, 0.2, 0.4, 0.6, 0.8, 1]}
          tickFormatter={(v: number) => pct(v, 0)}
          tick={AXIS_TICK}
          stroke="#525252"
          tickLine={false}
          axisLine={false}
          label={{
            ...AXIS_LABEL,
            value: 'Home team won',
            angle: -90,
            position: 'insideLeft',
            offset: 4,
          }}
        />
        <ReferenceLine
          segment={[
            { x: 0, y: 0 },
            { x: 1, y: 1 },
          ]}
          stroke="currentColor"
          strokeDasharray="4 4"
        />
        <Tooltip
          content={({ active, payload }) => {
            if (!active || !payload?.length) return null;
            const b = payload[0].payload as CalibrationRow;
            return (
              <div className={TOOLTIP_CLASS}>
                <div>Predicted {b.label}</div>
                <div>Average prediction: {pct(b.meanPredicted)}</div>
                <div>Home won: {pct(b.homeWinRate)}</div>
                <div>{b.games.toLocaleString()} games</div>
              </div>
            );
          }}
        />
        <Line
          dataKey="homeWinRate"
          stroke={color}
          strokeWidth={2.5}
          isAnimationActive={false}
          dot={false}
          activeDot={false}
        />
        <Scatter dataKey="homeWinRate" fill={color} isAnimationActive={false} />
      </ComposedChart>
    </ResponsiveContainer>
  );
}

export function LogLossPlot({ data, series }: { data: LogLossRow[]; series: LogLossSeries[] }) {
  return (
    <ResponsiveContainer width="100%" height={300}>
      <LineChart data={data} margin={{ top: 8, right: 20, bottom: 8, left: 0 }}>
        <XAxis
          dataKey="date"
          tick={AXIS_TICK}
          interval="preserveStartEnd"
          minTickGap={24}
          stroke="#525252"
          tickLine={false}
          axisLine={false}
        />
        <YAxis
          domain={['auto', 'auto']}
          tickFormatter={(v: number) => v.toFixed(3)}
          tick={AXIS_TICK}
          stroke="#525252"
          tickLine={false}
          axisLine={false}
        />
        <ReferenceLine y={COIN_FLIP} stroke="currentColor" strokeDasharray="2 3" />
        <Tooltip
          content={({ active, payload, label }) => {
            if (!active || !payload?.length) return null;
            const d = payload[0].payload as LogLossRow;
            return (
              <div className={TOOLTIP_CLASS}>
                <div>
                  {label} · {d.games.toLocaleString()} games
                </div>
                {series
                  .filter((s) => d[s.name] != null)
                  .map((s) => (
                    <div key={s.name} className="flex items-center gap-1.5">
                      <LineKey
                        color={readableLineColor(
                          s.name === MODEL ? MODEL_COLOR : MARKET_COLOR,
                          true,
                        )}
                        dash={s.dash}
                      />
                      {s.name}: {ll(d[s.name] as number)}
                    </div>
                  ))}
              </div>
            );
          }}
        />
        {series.map((s) => (
          <Line
            key={s.name}
            type="monotone"
            dataKey={s.name}
            stroke={s.color}
            strokeDasharray={s.dash || undefined}
            // The draw-in animation overwrites strokeDasharray
            isAnimationActive={false}
            strokeWidth={2.5}
            dot={false}
            activeDot={{ r: 4, fill: s.color, stroke: '#141418', strokeWidth: 2 }}
            connectNulls
          />
        ))}
      </LineChart>
    </ResponsiveContainer>
  );
}
