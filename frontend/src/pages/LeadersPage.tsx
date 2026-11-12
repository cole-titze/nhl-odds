import { useState } from 'react';
import { Link, useLoaderData, useSearchParams, type LoaderFunctionArgs } from 'react-router';
import { getStatLeaders } from '../api/leaders';
import { forwardClientIp } from '../api/client';
import { SeasonSelector } from '../components/SeasonSelector';
import { Skeleton } from '../components/Skeleton';
import { TeamLogo } from '../components/TeamLogo';
import { LoadingStatus, PageTitle, ScrollRegion } from '../components/A11y';
import { useFetch } from '../hooks/useFetch';
import { DEFAULT_LEADER_STAT, LEADER_STATS, leadersView } from '../utils/leaders';
import { formatSeasonLabel, getCurrentSeason } from '../utils/season';
import { itemList } from '../utils/structuredData';
import type { StatLeaderVM } from '../types';

const SHOWN = 10;

// The first load renders on the server (the stat and season come from the URL); client-side
// navigation and picking another stat or season fetch in the page
export async function loader({ request }: LoaderFunctionArgs) {
  const { stat, season } = leadersView(new URL(request.url).searchParams);
  return getStatLeaders(stat.key, season, forwardClientIp(request)).catch(() => null);
}

export async function clientLoader() {
  return null;
}

const th = 'py-2 px-3 font-semibold';

function LeaderRow({ leader, format }: { leader: StatLeaderVM; format: (v: number) => string }) {
  return (
    <tr className="border-t border-surface-200 dark:border-white/[0.06]">
      <td className="py-2 pr-3 text-right stat-number text-surface-500 dark:text-surface-400">
        {leader.rank}
      </td>
      <th scope="row" className="py-2 px-3 text-left font-normal">
        <Link to={`/player/${leader.playerId}`} className="flex items-center gap-3 group min-w-0">
          <img
            src={leader.headShot}
            alt=""
            loading="lazy"
            width={36}
            height={36}
            className="h-9 w-9 shrink-0 rounded-full object-cover bg-surface-200/60 dark:bg-white/[0.06]"
            onError={(e) => {
              (e.target as HTMLImageElement).style.visibility = 'hidden';
            }}
          />
          <span className="min-w-0">
            <span className="block truncate font-medium group-hover:text-accent-600 dark:group-hover:text-accent-400 group-hover:underline">
              {leader.firstName} {leader.lastName}
            </span>
            <span className="block text-xs text-surface-500 dark:text-surface-400">
              {leader.position}
            </span>
          </span>
        </Link>
      </th>
      <td className="py-2 px-3">
        {leader.teamId ? (
          <Link
            to={`/team/${leader.teamId}`}
            className="inline-flex items-center gap-1.5 hover:underline"
            title={leader.teamName}
          >
            <TeamLogo src={leader.teamLogoUri} alt="" className="h-5 w-5 shrink-0" />
            <span className="font-mono text-xs">{leader.teamAbbreviation}</span>
            <span className="sr-only"> ({leader.teamName})</span>
          </Link>
        ) : null}
      </td>
      <td className="py-2 px-3 text-right stat-number">{leader.gamesPlayed}</td>
      <td className="py-2 pl-3 text-right stat-number font-semibold">{format(leader.value)}</td>
    </tr>
  );
}

export function LeadersPage() {
  const initial = useLoaderData<typeof loader>();
  const [searchParams, setSearchParams] = useSearchParams();
  const { stat, season } = leadersView(searchParams);
  const [showAll, setShowAll] = useState(false);

  // In the URL, so a view can be linked and survives a reload; defaults are left out
  const setView = (next: { stat?: string; season?: number }) => {
    const s = next.stat ?? stat.key;
    const y = next.season ?? season;
    const params = new URLSearchParams();
    if (s !== DEFAULT_LEADER_STAT) params.set('stat', s);
    if (y !== getCurrentSeason()) params.set('season', String(y));
    setShowAll(false);
    setSearchParams(params, { preventScrollReset: true });
  };

  const { data, loading, error } = useFetch(
    () => getStatLeaders(stat.key, season),
    [stat.key, season],
    initial?.stat === stat.key && initial.seasonStartYear === season ? initial : null,
  );
  const leaders = data?.leaders ?? [];
  const shown = showAll ? leaders : leaders.slice(0, SHOWN);
  const caption = `${formatSeasonLabel(season)} regular season leaders in ${stat.label.toLowerCase()}`;
  const description =
    'NHL stat leaders for the regular season: points, goals, assists, shots, hits, save percentage, goals against average and more, with every player linked to their stats.';

  return (
    <div className="space-y-6">
      <PageTitle
        title="Stat Leaders"
        description={description}
        schema={
          data && data.leaders.length > 0
            ? [
                itemList(
                  `NHL leaders in ${data.label.toLowerCase()}`,
                  data.leaders.slice(0, SHOWN).map((l) => ({
                    name: `${l.firstName} ${l.lastName}`,
                    path: `/player/${l.playerId}`,
                  })),
                ),
              ]
            : []
        }
        imageAlt="NHL stat leaders"
      />
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-3">
        <h1 className="font-display text-3xl font-bold tracking-tight">Stat Leaders</h1>
        <div className="flex flex-wrap items-center gap-2">
          <select
            value={stat.key}
            onChange={(e) => setView({ stat: e.target.value })}
            aria-label="Stat"
            className="pl-3 pr-8 py-2 rounded-lg glass text-sm font-medium cursor-pointer appearance-none bg-[url('/chevron-down.svg')] bg-[length:16px] bg-[right_8px_center] bg-no-repeat"
          >
            {(['skater', 'goalie'] as const).map((group) => (
              <optgroup key={group} label={group === 'skater' ? 'Skaters' : 'Goalies'}>
                {LEADER_STATS.filter((s) => s.group === group).map((s) => (
                  <option key={s.key} value={s.key}>
                    {s.label}
                  </option>
                ))}
              </optgroup>
            ))}
          </select>
          <SeasonSelector value={season} onChange={(y) => setView({ season: y })} />
        </div>
      </div>
      <p className="text-surface-600 dark:text-surface-300 max-w-3xl">
        The regular season&apos;s leaders, from every game&apos;s box score. Each player&apos;s team
        is the one they played for most recently.
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
          <LoadingStatus label="Loading leaders…" />
          <Skeleton className="h-96 w-full rounded-xl" />
        </>
      )}
      {data && (
        <section aria-labelledby="leaders-heading" className="glass rounded-xl p-4 sm:p-6">
          <h2 id="leaders-heading" className="font-display text-xl font-semibold mb-1">
            {stat.label}
          </h2>
          {data.minGamesPlayed > 0 && (
            <p className="text-sm text-surface-500 dark:text-surface-400 mb-2">
              Players with at least {data.minGamesPlayed} games.
            </p>
          )}
          {leaders.length === 0 ? (
            <p className="text-surface-500 dark:text-surface-400 py-4">
              {season === getCurrentSeason()
                ? 'No games played yet this season.'
                : 'No games found for that season.'}
            </p>
          ) : (
            <>
              <ScrollRegion label={caption}>
                <table className="w-full text-sm">
                  <caption className="sr-only">{caption}</caption>
                  <thead>
                    <tr className="text-left text-xs uppercase tracking-wider text-surface-500 dark:text-surface-400">
                      <th scope="col" className={`${th} pl-0 text-right w-10`}>
                        <abbr title="Rank" className="no-underline">
                          #
                        </abbr>
                      </th>
                      <th scope="col" className={th}>
                        Player
                      </th>
                      <th scope="col" className={th}>
                        Team
                      </th>
                      <th scope="col" className={`${th} text-right`}>
                        <abbr title="Games played" className="no-underline">
                          GP
                        </abbr>
                      </th>
                      <th scope="col" className={`${th} pr-0 text-right`}>
                        <abbr title={stat.label} className="no-underline">
                          {stat.short}
                        </abbr>
                      </th>
                    </tr>
                  </thead>
                  <tbody>
                    {shown.map((l) => (
                      <LeaderRow key={l.playerId} leader={l} format={stat.format} />
                    ))}
                  </tbody>
                </table>
              </ScrollRegion>
              {leaders.length > SHOWN && (
                <button
                  type="button"
                  onClick={() => setShowAll((v) => !v)}
                  aria-expanded={showAll}
                  className="mt-3 text-sm font-medium text-accent-700 dark:text-accent-400 hover:underline"
                >
                  {showAll ? `Show top ${SHOWN}` : `Show top ${leaders.length}`}
                </button>
              )}
            </>
          )}
        </section>
      )}
    </div>
  );
}

export default LeadersPage;
