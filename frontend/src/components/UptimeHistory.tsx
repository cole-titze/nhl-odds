import { useEffect, useRef, useState, type KeyboardEvent } from 'react';
import { getUptimeDays, type UptimeDay } from '../api/admin';
import { ChartDataTable } from './ChartDataTable';

// The blackbox Probe's interval (ansible-setup prometheus.yml), so one probe is one minute
const PROBE_INTERVAL_SECONDS = 60;
const TIME_ZONE = 'America/Chicago';
const RANGES = [
  { days: 30, label: 'Last 30 days' },
  { days: 90, label: 'Last 90 days' },
  { days: 365, label: 'Last year' },
];
// A day missing more probes than this (a cluster or Prometheus outage, not a pod restart) is gray
// even with no failures: the site may well have been down too
const MAX_MISSING_PROBES = 15;
const SELECT_CLASS =
  "pl-3 pr-8 py-2 rounded-lg glass text-sm font-mono font-medium cursor-pointer appearance-none bg-[url('/chevron-down.svg')] bg-[length:16px] bg-[right_8px_center] bg-no-repeat";

type DayState = 'up' | 'partial' | 'down' | 'none';

// Status colors, the same in both modes; each bar's tooltip and the data table say it in words
const STATE_STYLES: Record<DayState, { bar: string; label: string }> = {
  up: { bar: 'bg-emerald-600', label: 'No downtime' },
  partial: { bar: 'bg-amber-500', label: 'Some downtime' },
  down: { bar: 'bg-red-600 dark:bg-red-500', label: 'Major downtime' },
  none: { bar: 'bg-surface-300 dark:bg-white/[0.12]', label: 'No data, or 15m+ missing' },
};

interface DayBar {
  day: string;
  state: DayState;
  data?: UptimeDay;
  uptime?: number;
  downProbes: number;
  missingProbes: number;
}

/** yyyy-mm-dd of today in Central Time, the time zone the rollup's days are in */
function centralToday() {
  return new Intl.DateTimeFormat('en-CA', { timeZone: TIME_ZONE }).format(new Date());
}

function addDays(day: string, n: number) {
  const d = new Date(`${day}T00:00:00Z`);
  d.setUTCDate(d.getUTCDate() + n);
  return d.toISOString().slice(0, 10);
}

function formatDay(day: string) {
  return new Date(`${day}T12:00:00Z`).toLocaleDateString(undefined, {
    timeZone: 'UTC',
    weekday: 'short',
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  });
}

function percent(value: number) {
  return `${(Math.floor(value * 10000) / 100).toFixed(2)}%`;
}

function minutes(probes: number) {
  const m = Math.round((probes * PROBE_INTERVAL_SECONDS) / 60);
  return m >= 60 ? `${Math.floor(m / 60)}h ${m % 60}m` : `${m}m`;
}

function toBar(day: string, data: UptimeDay | undefined): DayBar {
  if (!data || data.probes === 0) return { day, state: 'none', downProbes: 0, missingProbes: 0 };
  const uptime = data.successfulProbes / data.probes;
  const downProbes = data.probes - data.successfulProbes;
  const missingProbes = Math.max(0, data.expectedProbes - data.probes);
  // A failure outranks missing data
  const state: DayState =
    uptime < 0.99
      ? 'down'
      : downProbes > 0
        ? 'partial'
        : missingProbes > MAX_MISSING_PROBES
          ? 'none'
          : 'up';
  return { day, state, data, uptime, downProbes, missingProbes };
}

function describe(bar: DayBar) {
  if (!bar.data || bar.uptime == null) return `${formatDay(bar.day)}: no data`;
  const parts = [
    `${formatDay(bar.day)}: ${STATE_STYLES[bar.state].label}`,
    `${percent(bar.uptime)} up`,
    `${minutes(bar.downProbes)} down`,
  ];
  if (bar.missingProbes > 0) parts.push(`${minutes(bar.missingProbes)} no data`);
  if (bar.data.avgResponseMs != null)
    parts.push(`${Math.round(bar.data.avgResponseMs)} ms average`);
  return parts.join(', ');
}

function Tooltip({ bar, index, count }: { bar: DayBar; index: number; count: number }) {
  const center = ((index + 0.5) / count) * 100;
  // Keep the tooltip inside the chart near either end
  const shift = center < 20 ? '0%' : center > 80 ? '-100%' : '-50%';
  return (
    <div
      aria-hidden="true"
      className="pointer-events-none absolute bottom-full mb-2 z-10 w-56 rounded-lg border border-surface-200 dark:border-white/[0.08] bg-white dark:bg-surface-850 px-3 py-2 text-xs shadow-lg"
      // Set through the DOM: inline style attributes are off limits (lint rule, for the CSP)
      ref={(el) => {
        el?.style.setProperty('left', `${center}%`);
        el?.style.setProperty('transform', `translateX(${shift})`);
      }}
    >
      <div className="font-semibold mb-1">{formatDay(bar.day)}</div>
      <div className="flex items-center gap-2">
        <span className={`inline-block size-2.5 rounded-sm ${STATE_STYLES[bar.state].bar}`} />
        {STATE_STYLES[bar.state].label}
      </div>
      {bar.data && bar.uptime != null && (
        <dl className="mt-1 grid grid-cols-[auto_1fr] gap-x-3 text-surface-600 dark:text-surface-300">
          <dt>Uptime</dt>
          <dd className="stat-number text-right">{percent(bar.uptime)}</dd>
          <dt>Down</dt>
          <dd className="stat-number text-right">{minutes(bar.downProbes)}</dd>
          {bar.missingProbes > 0 && (
            <>
              <dt>No data</dt>
              <dd className="stat-number text-right">{minutes(bar.missingProbes)}</dd>
            </>
          )}
          {bar.data.avgResponseMs != null && (
            <>
              <dt>Avg response</dt>
              <dd className="stat-number text-right">{Math.round(bar.data.avgResponseMs)} ms</dd>
            </>
          )}
        </dl>
      )}
    </div>
  );
}

/** A bar per day of the public site's uptime, like a status page. */
export function UptimeHistory() {
  const [rangeDays, setRangeDays] = useState(30);
  const [days, setDays] = useState<UptimeDay[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [hovered, setHovered] = useState<number | null>(null);
  const [focused, setFocused] = useState<number | null>(null);
  const [tabStop, setTabStop] = useState<number | null>(null);
  const buttons = useRef<(HTMLButtonElement | null)[]>([]);

  useEffect(() => {
    let cancelled = false;
    async function load() {
      try {
        const data = await getUptimeDays(rangeDays);
        if (cancelled) return;
        setDays(data);
        setError(null);
      } catch (e) {
        if (!cancelled) setError(e instanceof Error ? e.message : 'Failed to load uptime');
      }
    }
    load();
    // The rollup runs every 15 minutes
    const interval = setInterval(load, 5 * 60 * 1000);
    return () => {
      cancelled = true;
      clearInterval(interval);
    };
  }, [rangeDays]);

  const today = centralToday();
  const byDay = new Map((days ?? []).map((d) => [d.day, d]));
  const bars = Array.from({ length: rangeDays }, (_, i) => {
    const day = addDays(today, i - rangeDays + 1);
    return toBar(day, byDay.get(day));
  });
  const measured = bars.filter((b) => b.data);
  const probes = measured.reduce((sum, b) => sum + b.data!.probes, 0);
  const successful = measured.reduce((sum, b) => sum + b.data!.successfulProbes, 0);
  const downProbes = measured.reduce((sum, b) => sum + b.downProbes, 0);
  // Gaps within measured days, plus whole days with no row since the first measured one (not today,
  // which only gets a row at the first rollup after midnight)
  const firstMeasured = bars.findIndex((b) => b.data);
  const missingProbes = bars.reduce(
    (sum, b, i) =>
      sum +
      (b.data
        ? b.missingProbes
        : firstMeasured >= 0 && i > firstMeasured && i < bars.length - 1
          ? (24 * 60 * 60) / PROBE_INTERVAL_SECONDS
          : 0),
    0,
  );
  const responseProbes = measured.filter((b) => b.data!.avgResponseMs != null);
  const avgResponse =
    responseProbes.reduce((sum, b) => sum + b.data!.avgResponseMs! * b.data!.probes, 0) /
    responseProbes.reduce((sum, b) => sum + b.data!.probes, 0);
  // Today's bar takes the single tab stop until another is focused
  const currentTabStop = tabStop != null && tabStop < bars.length ? tabStop : bars.length - 1;
  const shown = hovered ?? focused;

  function handleKeyDown(e: KeyboardEvent, index: number) {
    const next =
      e.key === 'ArrowRight'
        ? index + 1
        : e.key === 'ArrowLeft'
          ? index - 1
          : e.key === 'Home'
            ? 0
            : e.key === 'End'
              ? bars.length - 1
              : null;
    if (next == null || next < 0 || next >= bars.length) return;
    e.preventDefault();
    setTabStop(next);
    buttons.current[next]?.focus();
  }

  return (
    <div>
      <div className="flex flex-wrap items-center gap-x-6 gap-y-3 mb-4">
        <select
          value={rangeDays}
          onChange={(e) => {
            setRangeDays(Number(e.target.value));
            setTabStop(null);
          }}
          aria-label="Uptime range"
          className={SELECT_CLASS}
        >
          {RANGES.map((r) => (
            <option key={r.days} value={r.days}>
              {r.label}
            </option>
          ))}
        </select>
        {probes > 0 && (
          <dl className="flex flex-wrap gap-x-6 gap-y-1 text-sm">
            <div className="flex gap-2">
              <dt className="text-surface-500 dark:text-surface-400">Uptime</dt>
              <dd className="stat-number font-semibold">{percent(successful / probes)}</dd>
            </div>
            <div className="flex gap-2">
              <dt className="text-surface-500 dark:text-surface-400">Down</dt>
              <dd className="stat-number">{minutes(downProbes)}</dd>
            </div>
            <div className="flex gap-2">
              <dt className="text-surface-500 dark:text-surface-400">No data</dt>
              <dd className="stat-number">{minutes(missingProbes)}</dd>
            </div>
            {responseProbes.length > 0 && (
              <div className="flex gap-2">
                <dt className="text-surface-500 dark:text-surface-400">Avg response</dt>
                <dd className="stat-number">{Math.round(avgResponse)} ms</dd>
              </div>
            )}
          </dl>
        )}
      </div>

      <div className="glass rounded-xl p-4">
        {error ? (
          <p role="alert" className="text-sm text-red-700 dark:text-red-400 text-center py-4">
            {error}
          </p>
        ) : days == null ? (
          <p className="text-sm text-surface-500 dark:text-surface-400 text-center py-4">
            Loading uptime…
          </p>
        ) : (
          <>
            {measured.length === 0 && (
              <p className="text-sm text-surface-500 dark:text-surface-400 mb-3">
                No uptime data yet. The probe&apos;s results are rolled up every 15 minutes.
              </p>
            )}
            <div className="relative">
              {shown != null && bars[shown] && (
                <Tooltip bar={bars[shown]} index={shown} count={bars.length} />
              )}
              <ol
                aria-label={`Uptime per day, ${RANGES.find((r) => r.days === rangeDays)?.label.toLowerCase()}. Use the arrow keys to move between days.`}
                className={`flex h-9 items-stretch ${rangeDays > 90 ? 'gap-px' : 'gap-[2px]'}`}
                onMouseLeave={() => setHovered(null)}
              >
                {bars.map((bar, i) => (
                  <li key={bar.day} className="flex-1 min-w-0 flex">
                    <button
                      type="button"
                      ref={(el) => {
                        buttons.current[i] = el;
                      }}
                      tabIndex={i === currentTabStop ? 0 : -1}
                      aria-label={describe(bar)}
                      onMouseEnter={() => setHovered(i)}
                      onFocus={() => {
                        setFocused(i);
                        setTabStop(i);
                      }}
                      onBlur={() => setFocused(null)}
                      onKeyDown={(e) => handleKeyDown(e, i)}
                      className={`flex-1 min-w-0 rounded-sm ${STATE_STYLES[bar.state].bar} transition-opacity ${shown != null && shown !== i ? 'opacity-60' : ''} focus-visible:outline-2 focus-visible:outline-offset-2`}
                    />
                  </li>
                ))}
              </ol>
            </div>
            <div className="mt-2 flex justify-between text-xs text-surface-500 dark:text-surface-400">
              <span>{rangeDays - 1} days ago</span>
              <span>Today</span>
            </div>
            <ul className="mt-3 flex flex-wrap gap-x-4 gap-y-1 text-xs text-surface-600 dark:text-surface-300">
              {(Object.keys(STATE_STYLES) as DayState[]).map((state) => (
                <li key={state} className="flex items-center gap-1.5">
                  <span
                    aria-hidden="true"
                    className={`inline-block size-2.5 rounded-sm ${STATE_STYLES[state].bar}`}
                  />
                  {STATE_STYLES[state].label}
                  {state === 'down' && ' (under 99% up)'}
                </li>
              ))}
            </ul>
            <ChartDataTable
              caption="Uptime per day"
              rowHeader="Day"
              columns={['Uptime', 'Down (min)', 'No data (min)', 'Avg response']}
              rows={[...bars].reverse().map((bar) => ({
                key: bar.day,
                label: formatDay(bar.day),
                values: {
                  Uptime: bar.uptime,
                  'Down (min)': bar.data ? bar.downProbes : undefined,
                  'No data (min)': bar.data ? bar.missingProbes : undefined,
                  'Avg response': bar.data?.avgResponseMs ?? undefined,
                },
              }))}
              format={(value, column) =>
                column === 'Uptime'
                  ? percent(value)
                  : column === 'Avg response'
                    ? `${Math.round(value)} ms`
                    : String(Math.round((value * PROBE_INTERVAL_SECONDS) / 60))
              }
            />
          </>
        )}
      </div>
    </div>
  );
}
