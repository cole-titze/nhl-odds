import { lazy, type ReactNode } from 'react';
import { useSearchParams } from 'react-router';
import { SeasonSelector } from '../components/SeasonSelector';
import { Skeleton } from '../components/Skeleton';
import { LineKey } from '../components/LineKey';
import { ChartDataTable } from '../components/ChartDataTable';
import { LazyChart } from '../components/LazyChart';
import { LoadingStatus, PageTitle, ScrollRegion } from '../components/A11y';
import type { LogLossRow } from '../components/TrackRecordCharts';
import { useFetch } from '../hooks/useFetch';
import { useIsDarkTheme } from '../hooks/useRootTheme';
import { getTrackRecord } from '../api/trackRecord';
import { formatSeasonLabel, getCurrentSeason } from '../utils/season';
import { formatShortDate } from '../utils/dates';
import { dashFor, readableLineColor } from '../utils/chartSeries';
import { COIN_FLIP, MARKET, MARKET_COLOR, MODEL, MODEL_COLOR, ll, pct } from '../utils/trackRecord';
import type { CalibrationBinVM, ClosingLineVM, LogLossPointVM, TrackRecordVM } from '../types';

// Recharts (~100 KB) loads only when a chart scrolls near the screen (LazyChart), not with the page
const CalibrationPlot = lazy(() =>
  import('../components/TrackRecordCharts').then((m) => ({ default: m.CalibrationPlot })),
);
const LogLossPlot = lazy(() =>
  import('../components/TrackRecordCharts').then((m) => ({ default: m.LogLossPlot })),
);

export function TrackRecordPage() {
  // In the URL (?season=2024&live=1), so a view can be linked and survives a reload; defaults are left out
  const [searchParams, setSearchParams] = useSearchParams();
  const season = Number(searchParams.get('season')) || getCurrentSeason();
  const liveOnly = searchParams.get('live') === '1';
  const setView = (next: { season?: number; liveOnly?: boolean }) => {
    const s = next.season ?? season;
    const live = next.liveOnly ?? liveOnly;
    const params = new URLSearchParams();
    if (s !== getCurrentSeason()) params.set('season', String(s));
    if (live) params.set('live', '1');
    setSearchParams(params, { preventScrollReset: true });
  };
  const { data, loading, error } = useFetch(
    () => getTrackRecord(season, liveOnly),
    [season, liveOnly],
  );

  return (
    <div>
      <PageTitle
        title="Track Record"
        description="How well the NHL model's win probabilities hold up: calibration, log loss against sportsbooks and Kalshi, and closing line value."
      />
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-3 mb-6">
        <h1 className="font-display text-2xl font-bold tracking-tight">Track Record</h1>
        <SeasonSelector value={season} onChange={(s) => setView({ season: s })} />
      </div>

      <div
        className="flex rounded-lg glass p-1 gap-1 flex-wrap w-fit mb-3"
        role="group"
        aria-label="Predictions included"
      >
        {[
          { live: false, label: 'All predictions' },
          { live: true, label: 'Saved before puck drop' },
        ].map((option) => (
          <button
            key={option.label}
            type="button"
            aria-pressed={liveOnly === option.live}
            onClick={() => setView({ liveOnly: option.live })}
            className={`px-3 py-1 rounded-md text-sm font-medium transition-colors ${
              liveOnly === option.live
                ? 'bg-accent-600 text-on-accent'
                : 'text-surface-600 dark:text-surface-400 hover:text-surface-800 dark:hover:text-surface-200'
            }`}
          >
            {option.label}
          </button>
        ))}
      </div>

      {error && (
        <div
          role="alert"
          className="glass rounded-xl text-center text-red-700 dark:text-red-400 py-8 px-4 mb-8"
        >
          {error}
        </div>
      )}

      {loading && (
        <div className="space-y-4">
          <LoadingStatus label="Loading track record…" />
          <Skeleton className="h-4 w-2/3" />
          <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
            {Array.from({ length: 4 }, (_, i) => (
              <Skeleton key={i} className="h-20 w-full rounded-xl" />
            ))}
          </div>
          <Skeleton className="h-80 w-full rounded-xl" />
        </div>
      )}

      {!loading && !error && data && <TrackRecord data={data} />}
    </div>
  );
}

function TrackRecord({ data }: { data: TrackRecordVM }) {
  if (data.gameCount === 0)
    return (
      <p className="glass rounded-xl text-center text-surface-600 dark:text-surface-400 py-8 px-4">
        {data.seasonStartYear === getCurrentSeason()
          ? `No played games with a ${data.liveOnly ? 'prediction saved before puck drop' : 'prediction'} yet this season.`
          : `No games in ${formatSeasonLabel(data.seasonStartYear)} had a ${data.liveOnly ? 'prediction saved before puck drop' : 'prediction'}.`}
      </p>
    );

  const morning = data.sources[0]?.name.includes('(morning)') ? data.sources[0] : undefined;
  const backfilled = data.gameCount - data.liveGameCount;

  return (
    <div className="space-y-6">
      <p className="text-sm text-surface-600 dark:text-surface-400 leading-relaxed max-w-3xl">
        {data.liveOnly ? (
          <>
            Only predictions saved before puck drop: {data.gameCount.toLocaleString()} played games.
          </>
        ) : backfilled > 0 ? (
          <>
            {data.liveGameCount.toLocaleString()} of {data.gameCount.toLocaleString()} played games
            were predicted before puck drop. The other {backfilled.toLocaleString()} come from a
            walk-forward backfill: a model trained only on earlier games, but run after these games
            were played.
          </>
        ) : (
          <>All {data.gameCount.toLocaleString()} played games were predicted before puck drop.</>
        )}{' '}
        Bookmaker prices have their margin removed so they compare fairly with the model.
      </p>

      <div className="grid grid-cols-2 lg:grid-cols-4 gap-3">
        <Stat label="Games" value={data.gameCount.toLocaleString()} />
        <Stat
          label="Model log loss"
          value={data.modelLogLoss != null ? ll(data.modelLogLoss) : '–'}
          note={`Coin flip: ${ll(COIN_FLIP)}. Lower is better.`}
        />
        <Stat
          label="Model picked the winner"
          value={data.modelAccuracy != null ? pct(data.modelAccuracy) : '–'}
        />
        <Stat
          label="Bookmakers' log loss"
          value={morning ? ll(morning.logLoss) : '–'}
          note={
            morning
              ? `Model on the same ${morning.games.toLocaleString()} games: ${ll(morning.modelLogLoss)}`
              : 'No bookmaker prices yet'
          }
        />
      </div>

      <Card title="Calibration">
        <p className="text-sm text-surface-600 dark:text-surface-400 mb-4">
          Games grouped by the model&apos;s home win probability, against how often the home team
          actually won. A well-calibrated model sits on the dashed diagonal: teams it gives 60% win
          about 60% of the time. Small groups vary a lot, so check the game counts.
        </p>
        <CalibrationChart bins={data.calibration} />
      </Card>

      <Card title="Log loss over the season">
        <p className="text-sm text-surface-600 dark:text-surface-400 mb-4">
          Season-to-date average log loss after each game day. Lower is better; the dotted line is a
          coin flip. The bookmakers&apos; line only counts games they priced in the morning.
        </p>
        <LogLossChart points={data.logLossByDate} />
      </Card>

      <Card title="Against each market">
        <p className="text-sm text-surface-600 dark:text-surface-400 mb-4">
          Each row scores the model on exactly the games that market priced, so the two numbers
          compare fairly.
        </p>
        <SourcesTable data={data} />
      </Card>

      <Card title="Closing line value">
        <ClosingLine clv={data.closingLine} />
      </Card>
    </div>
  );
}

function Card({ title, children }: { title: string; children: ReactNode }) {
  return (
    <section className="glass rounded-xl p-5">
      <h2 className="text-xs font-semibold uppercase tracking-wider text-surface-600 dark:text-surface-400 mb-3">
        {title}
      </h2>
      {children}
    </section>
  );
}

function Stat({ label, value, note }: { label: string; value: string; note?: string }) {
  return (
    <div className="glass rounded-xl p-4">
      <div className="text-xs text-surface-600 dark:text-surface-400">{label}</div>
      <div className="font-mono text-xl font-semibold mt-1">{value}</div>
      {note && <div className="text-xs text-surface-600 dark:text-surface-400 mt-1">{note}</div>}
    </div>
  );
}

function CalibrationChart({ bins }: { bins: CalibrationBinVM[] }) {
  const dark = useIsDarkTheme();
  const color = readableLineColor(MODEL_COLOR, dark);
  const data = bins.map((b) => ({ ...b, label: `${pct(b.low, 0)}–${pct(b.high, 0)}` }));

  return (
    <>
      <div className="flex flex-wrap gap-4 text-xs font-mono mb-2" aria-hidden="true">
        <span className="inline-flex items-center gap-1.5">
          <LineKey color={color} dash="" />
          Home win rate
        </span>
        <span className="inline-flex items-center gap-1.5">
          <LineKey color="currentColor" dash="4 4" />
          Perfect calibration
        </span>
      </div>
      <div className="text-surface-600 dark:text-surface-400">
        <LazyChart boxClassName="h-[300px]">
          <CalibrationPlot data={data} color={color} />
        </LazyChart>
      </div>
      <ChartDataTable
        caption="Calibration by predicted home win probability"
        rowHeader="Predicted"
        columns={['Games', 'Average prediction', 'Home won']}
        rows={data.map((b) => ({
          key: b.label,
          label: b.label,
          values: {
            Games: b.games,
            'Average prediction': b.meanPredicted,
            'Home won': b.homeWinRate,
          },
        }))}
        format={(v, column) => (column === 'Games' ? v.toLocaleString() : pct(v))}
      />
    </>
  );
}

function LogLossChart({ points }: { points: LogLossPointVM[] }) {
  const dark = useIsDarkTheme();
  const series = [
    { name: MODEL, color: readableLineColor(MODEL_COLOR, dark), dash: dashFor(0) },
    { name: MARKET, color: readableLineColor(MARKET_COLOR, dark), dash: dashFor(1) },
  ];
  const data: LogLossRow[] = points.map((p) => ({
    key: p.date.slice(0, 10),
    date: formatShortDate(p.date.slice(0, 10)),
    games: p.games,
    [MODEL]: p.model,
    [MARKET]: p.market ?? undefined,
  }));

  if (data.length < 2)
    return (
      <p className="text-sm text-surface-600 dark:text-surface-400">
        The chart starts after a second game day.
      </p>
    );

  return (
    <>
      <div className="flex flex-wrap gap-4 text-xs font-mono mb-2" aria-hidden="true">
        {series.map((s) => (
          <span key={s.name} className="inline-flex items-center gap-1.5">
            <LineKey color={s.color} dash={s.dash} />
            {s.name}
          </span>
        ))}
      </div>
      <div className="text-surface-600 dark:text-surface-400">
        <LazyChart boxClassName="h-[300px]">
          <LogLossPlot data={data} series={series} />
        </LazyChart>
      </div>
      <ChartDataTable
        caption="Season-to-date log loss by game day"
        rowHeader="Date"
        columns={['Games', MODEL, MARKET]}
        rows={data.map((d) => ({
          key: d.key,
          label: d.date,
          values: {
            Games: d.games,
            [MODEL]: d[MODEL] as number,
            [MARKET]: d[MARKET] as number | undefined,
          },
        }))}
        format={(v, column) => (column === 'Games' ? v.toLocaleString() : ll(v))}
      />
    </>
  );
}

function SourcesTable({ data }: { data: TrackRecordVM }) {
  if (data.sources.length === 0)
    return (
      <p className="text-sm text-surface-600 dark:text-surface-400">
        No bookmaker prices for these games yet.
      </p>
    );

  return (
    <>
      <ScrollRegion label="Model against each market">
        <table className="w-full text-sm">
          <caption className="sr-only">
            Log loss and winner accuracy for each market and for the model on the same games
          </caption>
          <thead>
            <tr className="text-left text-xs text-surface-600 dark:text-surface-400">
              <th scope="col" className="py-2 pr-3 font-semibold">
                Market
              </th>
              <th scope="col" className="py-2 px-3 font-semibold text-right">
                Games
              </th>
              <th scope="col" className="py-2 px-3 font-semibold text-right">
                Market log loss
              </th>
              <th scope="col" className="py-2 px-3 font-semibold text-right">
                Model log loss
              </th>
              <th scope="col" className="py-2 px-3 font-semibold text-right">
                Market picked winner
              </th>
              <th scope="col" className="py-2 pl-3 font-semibold text-right">
                Model picked winner
              </th>
            </tr>
          </thead>
          <tbody className="font-mono">
            {data.sources.map((s) => {
              const modelBetter = s.modelLogLoss < s.logLoss;
              return (
                <tr
                  key={s.name}
                  className="border-t border-surface-200/60 dark:border-white/[0.06]"
                >
                  <th scope="row" className="py-2 pr-3 text-left font-sans font-medium">
                    {s.name}
                  </th>
                  <td className="py-2 px-3 text-right">{s.games.toLocaleString()}</td>
                  <td className={`py-2 px-3 text-right ${modelBetter ? '' : 'font-bold'}`}>
                    {ll(s.logLoss)}
                  </td>
                  <td className={`py-2 px-3 text-right ${modelBetter ? 'font-bold' : ''}`}>
                    {ll(s.modelLogLoss)}
                    <span className="sr-only">{modelBetter ? ' (model lower)' : ''}</span>
                  </td>
                  <td className="py-2 px-3 text-right">{pct(s.accuracy)}</td>
                  <td className="py-2 pl-3 text-right">{pct(s.modelAccuracy)}</td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </ScrollRegion>
      <p className="text-xs text-surface-600 dark:text-surface-400 mt-2">
        The lower log loss in each row is in bold.
      </p>
    </>
  );
}

function ClosingLine({ clv }: { clv: ClosingLineVM | null }) {
  const intro = (
    <p className="text-sm text-surface-600 dark:text-surface-400 mb-4 max-w-3xl">
      Prices move between the morning and puck drop as the market learns more, and the closing price
      is usually the best estimate. If the market tends to move toward the model during the day, the
      model is seeing something real; if it moves away, the model is likely just noisy. This uses
      bookmakers with both a morning and a closing price for the game.
    </p>
  );
  if (!clv)
    return (
      <>
        {intro}
        <p className="text-sm text-surface-600 dark:text-surface-400">
          No games with both morning and closing prices yet.
        </p>
      </>
    );

  const unchanged = clv.games - clv.movedTowardModel - clv.movedAwayFromModel;
  const points = clv.averageMoveTowardModel * 100;
  return (
    <>
      {intro}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-3">
        <Stat label="Games" value={clv.games.toLocaleString()} />
        <Stat
          label="Model closer to the close"
          value={pct(clv.modelCloser / clv.games)}
          note="than the morning price was"
        />
        <Stat
          label="Moved toward the model"
          value={pct(clv.movedTowardModel / clv.games)}
          note={`Away: ${pct(clv.movedAwayFromModel / clv.games)}${unchanged > 0 ? `, unchanged: ${pct(unchanged / clv.games)}` : ''}`}
        />
        <Stat
          label="Average move"
          value={`${points >= 0 ? '+' : '−'}${Math.abs(points).toFixed(2)} pts`}
          note="toward the model (negative: away)"
        />
      </div>
    </>
  );
}

export default TrackRecordPage;
