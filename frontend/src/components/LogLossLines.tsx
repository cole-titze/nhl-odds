// The Recharts part of LogLossChart. LogLossChart loads this module lazily (inside LazyChart), so
// the chart library downloads and draws only when the chart scrolls near the screen.
import { LineChart, Line, XAxis, YAxis, Tooltip, ResponsiveContainer } from 'recharts';
import { LineKey } from './LineKey';

const AXIS_TICK = { fontSize: 11, fontFamily: 'JetBrains Mono Variable', fill: 'currentColor' };

export type LogLossPoint = Record<string, string | number | undefined>;
/** One shown line: color on the page, color in the (always dark) tooltip, dash pattern */
export type LogLossLine = { name: string; color: string; tooltipColor: string; dash: string };

export default function LogLossLines({
  data,
  lines,
}: {
  data: LogLossPoint[];
  lines: LogLossLine[];
}) {
  return (
    <ResponsiveContainer width="100%" height={250}>
      <LineChart data={data}>
        <XAxis
          dataKey="date"
          tick={AXIS_TICK}
          interval="preserveStartEnd"
          stroke="#525252"
          tickLine={false}
          axisLine={false}
        />
        <YAxis
          tick={AXIS_TICK}
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
                {lines
                  .filter((line) => d[line.name] != null)
                  .map((line) => (
                    <div key={line.name} className="flex items-center gap-1.5">
                      <LineKey color={line.tooltipColor} dash={line.dash} />
                      {line.name}: {d[line.name] as number}
                    </div>
                  ))}
                <div>games: {d.games as number}</div>
              </div>
            );
          }}
        />
        {lines.map((line) => (
          <Line
            key={line.name}
            type="monotone"
            dataKey={line.name}
            stroke={line.color}
            strokeDasharray={line.dash || undefined}
            // Recharts' draw-in animation overwrites strokeDasharray (and
            // blanks it under prefers-reduced-motion), erasing the patterns
            isAnimationActive={false}
            strokeWidth={2.5}
            dot={false}
            activeDot={{ r: 4, fill: line.color, stroke: '#141418', strokeWidth: 2 }}
            connectNulls
          />
        ))}
      </LineChart>
    </ResponsiveContainer>
  );
}
