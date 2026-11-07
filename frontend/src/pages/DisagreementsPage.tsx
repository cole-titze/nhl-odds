import { useMemo, useState } from 'react';
import { Link, useLoaderData, type LoaderFunctionArgs } from 'react-router';
import { parseISO } from 'date-fns';
import { getGameOddsInDateRange } from '../api/gameOdds';
import { forwardClientIp } from '../api/client';
import { SeasonSelector } from '../components/SeasonSelector';
import { Skeleton } from '../components/Skeleton';
import { TeamLogo } from '../components/TeamLogo';
import { LoadingStatus, PageTitle, ScrollRegion } from '../components/A11y';
import { useFetch } from '../hooks/useFetch';
import { useOddsFormatContext } from '../contexts/OddsFormatContext';
import { formatOdds } from '../utils/oddsFormat';
import { formatDisplayDate } from '../utils/dates';
import { getCurrentSeason } from '../utils/season';
import {
  TOP_PER_DAY,
  buildDisagreements,
  sideProbability,
  type DisagreementRecord,
  type DisagreementRow,
  type DisagreementsData,
} from '../utils/marketDisagreements';

function loadDisagreements(season: number, init?: RequestInit): Promise<DisagreementsData> {
  return getGameOddsInDateRange(`${season}-09-01`, `${season + 1}-07-31`, season, init).then(
    (games) => buildDisagreements(games),
  );
}

// First load renders on the server, sending only the rows the page shows rather
// than the season's games. Client-side navigation skips it and the page fetches.
export async function loader({ request }: LoaderFunctionArgs) {
  return loadDisagreements(getCurrentSeason(), forwardClientIp(request)).catch(() => null);
}

export async function clientLoader() {
  return null;
}

const pts = (p: number) => `${(p * 100).toFixed(1)} pts`;
const pct = (n: number, d: number) => (d > 0 ? `${((n / d) * 100).toFixed(1)}%` : '—');

function Matchup({ row }: { row: DisagreementRow }) {
  const team = (t: DisagreementRow['home'], favored: boolean) => (
    <span className={`inline-flex items-center gap-1.5 ${favored ? 'font-semibold' : ''}`}>
      <TeamLogo src={t.logoUri} alt="" className="h-5 w-5 shrink-0" />
      {t.name}
      {favored && <span className="sr-only"> (model&apos;s side)</span>}
      {row.played && (
        <span className="font-mono text-surface-500 dark:text-surface-400">{t.goals}</span>
      )}
    </span>
  );
  return (
    <Link
      to={`/game/${row.gameId}`}
      className="flex flex-wrap items-center gap-x-2 gap-y-1 hover:underline"
    >
      {team(row.away, row.side === 'away')}
      <span className="text-surface-500 dark:text-surface-400">at</span>
      {team(row.home, row.side === 'home')}
    </Link>
  );
}

function Result({ row }: { row: DisagreementRow }) {
  if (row.sideWon == null)
    return <span className="text-surface-500 dark:text-surface-400">Upcoming</span>;
  return row.sideWon ? (
    <span className="text-emerald-700 dark:text-emerald-400">Won</span>
  ) : (
    <span className="text-red-700 dark:text-red-400">Lost</span>
  );
}

function RowsTable({ rows, label }: { rows: DisagreementRow[]; label: string }) {
  const { format } = useOddsFormatContext();
  return (
    <ScrollRegion label={label}>
      <table className="w-full text-sm">
        <caption className="sr-only">{label}</caption>
        <thead>
          <tr className="text-left text-xs uppercase tracking-wider text-surface-500 dark:text-surface-400">
            <th scope="col" className="py-2 pr-3 font-semibold">
              Game
            </th>
            <th scope="col" className="py-2 px-3 font-semibold text-right">
              Model
            </th>
            <th scope="col" className="py-2 px-3 font-semibold text-right">
              Market
            </th>
            <th scope="col" className="py-2 px-3 font-semibold text-right">
              Gap
            </th>
            <th scope="col" className="py-2 pl-3 font-semibold text-right">
              Result
            </th>
          </tr>
        </thead>
        <tbody>
          {rows.map((r) => (
            <tr key={r.gameId} className="border-t border-surface-200 dark:border-white/[0.06]">
              <td className="py-2 pr-3">
                <Matchup row={r} />
              </td>
              <td className="py-2 px-3 text-right font-mono">
                {formatOdds(sideProbability(r, r.modelHome), format)}
              </td>
              <td
                className="py-2 px-3 text-right font-mono"
                title={`Average of ${r.books.join(', ')}`}
              >
                {formatOdds(sideProbability(r, r.marketHome), format)}
              </td>
              <td className="py-2 px-3 text-right font-mono whitespace-nowrap">{pts(r.gap)}</td>
              <td className="py-2 pl-3 text-right">
                <Result row={r} />
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </ScrollRegion>
  );
}

function RecordSummary({ record }: { record: DisagreementRecord }) {
  if (record.games === 0) {
    return (
      <p className="text-surface-500 dark:text-surface-400">No played games yet this season.</p>
    );
  }
  const stat = (value: string, label: string) => (
    <div className="glass rounded-xl p-4 text-center">
      <div className="stat-number text-2xl text-surface-900 dark:text-white">{value}</div>
      <div className="text-[10px] sm:text-xs font-medium text-surface-500 dark:text-surface-400 mt-1 uppercase tracking-wider">
        {label}
      </div>
    </div>
  );
  return (
    <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
      {stat(String(record.games), 'Games')}
      {stat(pct(record.sideWins, record.games), "Model's side won")}
      {stat(pct(record.modelExpected, record.games), 'Model expected')}
      {stat(pct(record.marketExpected, record.games), 'Market expected')}
    </div>
  );
}

export function DisagreementsPage() {
  const initial = useLoaderData<typeof loader>();
  const [season, setSeason] = useState(getCurrentSeason);
  const { data, loading, error } = useFetch(() => loadDisagreements(season), [season], initial);
  const bands = useMemo(() => data?.bands.filter((b) => b.games > 0) ?? [], [data]);

  return (
    <div className="space-y-8">
      <PageTitle
        title="Model vs Market"
        description="The NHL games where the NHL Odds model and the betting market disagree most each day, and how those games turned out."
      />
      <div className="flex items-center justify-between gap-4">
        <h1 className="font-display text-3xl font-bold tracking-tight">Model vs Market</h1>
        <SeasonSelector value={season} onChange={setSeason} />
      </div>
      <p className="text-surface-600 dark:text-surface-300 max-w-3xl">
        Each game&apos;s gap between the model&apos;s win probability and the market&apos;s. The
        market is the average of every book&apos;s probability with its margin removed. The
        model&apos;s side (in bold in the tables) is the team it rates higher than the market does,
        and its result is whether that team won. This is data on where the two differ, not betting
        advice.
      </p>

      {error && (
        <div
          role="alert"
          className="glass rounded-xl text-center text-red-700 dark:text-red-400 py-8"
        >
          {error}
        </div>
      )}

      {loading && (
        <>
          <LoadingStatus label="Loading disagreements…" />
          <Skeleton className="h-64 w-full rounded-xl" />
          <Skeleton className="h-28 w-full rounded-xl" />
        </>
      )}

      {data && (
        <>
          <section aria-labelledby="day-heading" className="glass rounded-xl p-4 sm:p-6">
            <h2 id="day-heading" className="font-display text-xl font-semibold mb-3">
              {data.day ? formatDisplayDate(parseISO(data.day)) : 'No games'}
            </h2>
            {data.dayRows.length > 0 ? (
              <RowsTable rows={data.dayRows} label="Games by disagreement, largest first" />
            ) : (
              <p className="text-surface-500 dark:text-surface-400">
                No games with both a model prediction and market odds this season.
              </p>
            )}
          </section>

          <section aria-labelledby="record-heading" className="space-y-4">
            <h2 id="record-heading" className="font-display text-xl font-semibold">
              Record: each day&apos;s {TOP_PER_DAY} biggest disagreements
            </h2>
            <RecordSummary record={data.topRecord} />
            {data.topRecord.games > 0 && (
              <p className="text-sm text-surface-600 dark:text-surface-300">
                Log loss on these games: model {data.topRecord.modelLogLoss.toFixed(4)}, market{' '}
                {data.topRecord.marketLogLoss.toFixed(4)} (lower is better).
              </p>
            )}
          </section>

          {bands.length > 0 && (
            <section aria-labelledby="bands-heading" className="glass rounded-xl p-4 sm:p-6">
              <h2 id="bands-heading" className="font-display text-xl font-semibold mb-3">
                Every played game, by gap size
              </h2>
              <ScrollRegion label="Record by gap size">
                <table className="w-full text-sm">
                  <caption className="sr-only">Record by gap size</caption>
                  <thead>
                    <tr className="text-left text-xs uppercase tracking-wider text-surface-500 dark:text-surface-400">
                      <th scope="col" className="py-2 pr-3 font-semibold">
                        Gap
                      </th>
                      <th scope="col" className="py-2 px-3 font-semibold text-right">
                        Games
                      </th>
                      <th scope="col" className="py-2 px-3 font-semibold text-right">
                        Model&apos;s side won
                      </th>
                      <th scope="col" className="py-2 px-3 font-semibold text-right">
                        Model expected
                      </th>
                      <th scope="col" className="py-2 pl-3 font-semibold text-right">
                        Market expected
                      </th>
                    </tr>
                  </thead>
                  <tbody>
                    {bands.map((b) => (
                      <tr
                        key={b.label}
                        className="border-t border-surface-200 dark:border-white/[0.06]"
                      >
                        <th scope="row" className="py-2 pr-3 text-left font-medium">
                          {b.label}
                        </th>
                        <td className="py-2 px-3 text-right font-mono">{b.games}</td>
                        <td className="py-2 px-3 text-right font-mono">
                          {pct(b.sideWins, b.games)}
                        </td>
                        <td className="py-2 px-3 text-right font-mono">
                          {pct(b.modelExpected, b.games)}
                        </td>
                        <td className="py-2 pl-3 text-right font-mono">
                          {pct(b.marketExpected, b.games)}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </ScrollRegion>
            </section>
          )}

          {data.recent.length > 0 && (
            <section aria-labelledby="recent-heading" className="space-y-4">
              <h2 id="recent-heading" className="font-display text-xl font-semibold">
                Recent days
              </h2>
              {data.recent.map(({ day, rows }) => (
                <div key={day} className="glass rounded-xl p-4 sm:p-6">
                  <h3 className="font-semibold mb-2">{formatDisplayDate(parseISO(day))}</h3>
                  <RowsTable
                    rows={rows}
                    label={`Biggest disagreements on ${formatDisplayDate(parseISO(day))}`}
                  />
                </div>
              ))}
            </section>
          )}
        </>
      )}
    </div>
  );
}

export default DisagreementsPage;
