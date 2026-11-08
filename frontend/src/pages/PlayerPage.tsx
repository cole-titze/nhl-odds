import {
  data,
  Link,
  useLoaderData,
  useParams,
  useSearchParams,
  type LoaderFunctionArgs,
} from 'react-router';
import { useFetch } from '../hooks/useFetch';
import { getPlayer } from '../api/players';
import { forwardClientIp } from '../api/client';
import { getCurrentSeason, formatSeasonLabel } from '../utils/season';
import { formatShortDate } from '../utils/dates';
import { useDisplayTimeZone } from '../hooks/useDisplayTimeZone';
import {
  formatHeight,
  formatPlusMinus,
  formatSavePct,
  formatToi,
  playerName,
  positionName,
} from '../utils/players';
import type { PlayerGameVM, PlayerVM } from '../types';
import { Skeleton } from '../components/Skeleton';
import { SeasonSelector } from '../components/SeasonSelector';
import { StatCard } from '../components/StatCard';
import { TeamLogo } from '../components/TeamLogo';
import { InjuredReserveBadge } from '../components/InjuredReserveBadge';
import { Breadcrumbs, type Crumb } from '../components/Breadcrumbs';
import { LoadingStatus, PageTitle, ScrollRegion } from '../components/A11y';
import { breadcrumbs, person } from '../utils/structuredData';

function seasonFrom(searchParams: URLSearchParams) {
  return Number(searchParams.get('season')) || getCurrentSeason();
}

// First load renders on the server; client-side navigation fetches in the page.
// An unknown player still renders the page's "Player not found" message, with a
// real 404 status for crawlers.
export async function loader({ params, request }: LoaderFunctionArgs) {
  const season = seasonFrom(new URL(request.url).searchParams);
  let player: PlayerVM | undefined;
  try {
    player = await getPlayer(params.playerId!, season, forwardClientIp(request));
  } catch {
    return null;
  }
  return player ? { player } : data({ player: null }, { status: 404 });
}

export async function clientLoader() {
  return null;
}

const th = 'py-3 px-3 font-semibold';
const thNum = `${th} text-center`;
const td = 'py-2.5 px-3';
const tdNum = `${td} text-center stat-number`;

const RESULT_LABELS = { W: 'Win', L: 'Loss', OTL: 'Overtime loss' } as const;

function ResultBadge({ game }: { game: PlayerGameVM }) {
  const won = game.result === 'W';
  return (
    <span className="inline-flex items-center gap-1.5">
      <span
        className={`inline-block min-w-8 px-1.5 py-0.5 rounded text-center text-xs font-bold ${
          won
            ? 'bg-blue-500/10 text-blue-700 dark:text-blue-400'
            : 'bg-red-500/10 text-red-700 dark:text-red-400'
        }`}
      >
        <span aria-hidden="true">{game.result}</span>
        <span className="sr-only">{RESULT_LABELS[game.result]}</span>
      </span>
      <span className="stat-number text-xs">
        {game.teamGoals}-{game.opponentGoals}
      </span>
    </span>
  );
}

export function PlayerPage() {
  const loaded = useLoaderData<typeof loader>();
  const { playerId } = useParams();
  const { timeZone, locales } = useDisplayTimeZone();
  const [searchParams, setSearchParams] = useSearchParams();
  const season = seasonFrom(searchParams);

  // Wrapped so the server's "no such player" (player: null) counts as loaded data
  const {
    data: fetched,
    loading,
    error,
  } = useFetch(
    async () => ({ player: (await getPlayer(playerId ?? '', season)) ?? null }),
    [playerId, season],
    loaded,
  );
  const player = fetched?.player;

  if (loading) {
    return (
      <div className="space-y-4">
        <PageTitle title="Player" />
        <LoadingStatus label="Loading player…" />
        <Skeleton className="h-6 w-1/4" />
        <Skeleton className="h-24 w-1/2" />
        <Skeleton className="h-64 w-full" />
      </div>
    );
  }

  if (error)
    return (
      <div
        role="alert"
        className="glass rounded-xl text-center text-red-700 dark:text-red-400 py-8"
      >
        {error}
      </div>
    );
  if (!player)
    return (
      <div className="text-center text-surface-500 dark:text-surface-400 py-12">
        <PageTitle title="Player not found" noindex />
        Player not found.
      </div>
    );

  const name = playerName(player);
  const team = player.currentTeam;
  const teamName = team ? `${team.locationName} ${team.teamName}` : null;
  // On no NHL roster now (sent down, unsigned, retired): who they last played for
  const lastTeam = team ? null : player.lastTeam;
  const lastTeamName = lastTeam ? `${lastTeam.locationName} ${lastTeam.teamName}` : null;
  const crumbTeam = team ?? lastTeam;
  const isGoalie = player.position === 'G';
  const crumbs: Crumb[] = [
    { name: 'Teams', path: '/teams' },
    ...(crumbTeam
      ? [{ name: `${crumbTeam.locationName} ${crumbTeam.teamName}`, path: `/team/${crumbTeam.id}` }]
      : []),
    { name, path: `/player/${player.id}` },
  ];
  const totals = player.seasonTotals;
  const born = player.birthDate
    ? new Date(`${player.birthDate}T12:00:00Z`).toLocaleDateString(locales, {
        month: 'short',
        day: 'numeric',
        year: 'numeric',
        timeZone: 'UTC',
      })
    : null;
  const birthPlace = [player.birthCity, player.birthStateProvince].filter(Boolean).join(', ');
  const bio = [
    born && `Born ${born}${birthPlace ? ` in ${birthPlace}` : ''}`,
    formatHeight(player.heightInInches),
    player.weightInPounds > 0 && `${player.weightInPounds} lb`,
  ].filter(Boolean);
  const seasonLabel = formatSeasonLabel(season);
  const description = `${name}${positionName(player.position) ? `, ${positionName(player.position).toLowerCase()}` : ''}${
    teamName ? ` for the ${teamName}` : lastTeamName ? `, last with the ${lastTeamName}` : ''
  }: ${seasonLabel} NHL game log and stats.`;

  return (
    <div>
      <PageTitle
        title={name}
        description={description}
        schema={[person(player), breadcrumbs(crumbs)]}
      />
      <Breadcrumbs items={crumbs} />

      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between mb-8 gap-4">
        <div className="flex items-center gap-5 min-w-0">
          {player.headShot && (
            <img
              src={player.headShot}
              alt=""
              className="h-24 w-24 shrink-0 rounded-full object-cover bg-surface-200/60 dark:bg-white/[0.06]"
              onError={(e) => {
                (e.target as HTMLImageElement).style.display = 'none';
              }}
            />
          )}
          <div className="min-w-0">
            <h1 className="font-display text-3xl font-bold tracking-tight">{name}</h1>
            <p className="mt-1 flex flex-wrap items-center gap-x-2 text-sm text-surface-600 dark:text-surface-300">
              {player.sweaterNumber != null && (
                <span className="stat-number">#{player.sweaterNumber}</span>
              )}
              {player.position && <span>{positionName(player.position)}</span>}
              {team && teamName && (
                <Link
                  to={`/team/${team.id}`}
                  className="inline-flex items-center gap-1.5 py-0.5 font-medium underline-offset-2 hover:underline hover:text-accent-600 dark:hover:text-accent-400"
                >
                  <TeamLogo src={team.logoUri} alt="" className="h-5 w-5 object-contain" />
                  {teamName}
                </Link>
              )}
              {team && player.isInjuredReserve && <InjuredReserveBadge />}
              {lastTeam && lastTeamName && (
                <span className="inline-flex items-center gap-1.5">
                  Last played for
                  <Link
                    to={`/team/${lastTeam.id}`}
                    className="inline-flex items-center gap-1.5 py-0.5 font-medium underline-offset-2 hover:underline hover:text-accent-600 dark:hover:text-accent-400"
                  >
                    <TeamLogo src={lastTeam.logoUri} alt="" className="h-5 w-5 object-contain" />
                    {lastTeamName}
                  </Link>
                  {player.lastSeasonStartYear != null &&
                    player.lastSeasonStartYear !== getCurrentSeason() &&
                    `in ${formatSeasonLabel(player.lastSeasonStartYear)}`}
                </span>
              )}
            </p>
            {bio.length > 0 && (
              <p className="mt-1 text-xs text-surface-500 dark:text-surface-400">
                {bio.join(' · ')}
              </p>
            )}
          </div>
        </div>
        <SeasonSelector value={season} onChange={(s) => setSearchParams({ season: String(s) })} />
      </div>

      {totals.gamesPlayed > 0 && (
        <section aria-label={`${seasonLabel} totals`} className="mb-8">
          {isGoalie ? (
            <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
              <StatCard label="Games" value={String(totals.gamesPlayed)} />
              <StatCard label="Save %" value={formatSavePct(totals.savePct)} accent />
              <StatCard
                label="Goals against average"
                value={
                  totals.goalsAgainstAverage != null ? totals.goalsAgainstAverage.toFixed(2) : '-'
                }
              />
              <StatCard
                label="Saves"
                value={String(totals.saves)}
                sub={`${totals.shotsAgainst} shots`}
              />
            </div>
          ) : (
            <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-3">
              <StatCard label="Games" value={String(totals.gamesPlayed)} />
              <StatCard label="Goals" value={String(totals.goals)} />
              <StatCard label="Assists" value={String(totals.assists)} />
              <StatCard label="Points" value={String(totals.points)} accent />
              <StatCard label="Shots" value={String(totals.shotsOnGoal)} />
              <StatCard
                label="Time on ice per game"
                value={formatToi(totals.timeOnIceSeconds / totals.gamesPlayed)}
              />
            </div>
          )}
        </section>
      )}

      {player.games.length > 0 ? (
        <div className="glass rounded-xl overflow-hidden">
          <h2 className="px-4 pt-4 pb-2 text-xs font-mono font-bold tracking-widest uppercase text-surface-500 dark:text-surface-400">
            {seasonLabel} games
          </h2>
          <ScrollRegion label="Game log">
            <table className="w-full text-sm">
              <caption className="sr-only">{seasonLabel} game log, newest first</caption>
              <thead>
                <tr className="border-b border-surface-200 dark:border-white/[0.06] text-left text-xs uppercase tracking-wider text-surface-500 dark:text-surface-400">
                  <th scope="col" className={th}>
                    Date
                  </th>
                  <th scope="col" className={th}>
                    Opponent
                  </th>
                  <th scope="col" className={th}>
                    Result
                  </th>
                  {isGoalie ? (
                    <>
                      <th scope="col" className={thNum}>
                        <abbr title="Shots against" className="no-underline">
                          SA
                        </abbr>
                      </th>
                      <th scope="col" className={thNum}>
                        <abbr title="Saves" className="no-underline">
                          SV
                        </abbr>
                      </th>
                      <th scope="col" className={thNum}>
                        <abbr title="Goals against" className="no-underline">
                          GA
                        </abbr>
                      </th>
                      <th scope="col" className={thNum}>
                        <abbr title="Save percentage" className="no-underline">
                          SV%
                        </abbr>
                      </th>
                    </>
                  ) : (
                    <>
                      <th scope="col" className={thNum}>
                        <abbr title="Goals" className="no-underline">
                          G
                        </abbr>
                      </th>
                      <th scope="col" className={thNum}>
                        <abbr title="Assists" className="no-underline">
                          A
                        </abbr>
                      </th>
                      <th scope="col" className={thNum}>
                        <abbr title="Points" className="no-underline">
                          P
                        </abbr>
                      </th>
                      <th scope="col" className={thNum}>
                        <abbr title="Shots on goal" className="no-underline">
                          SOG
                        </abbr>
                      </th>
                      <th scope="col" className={thNum}>
                        <abbr title="Plus/minus" className="no-underline">
                          +/-
                        </abbr>
                      </th>
                    </>
                  )}
                  <th scope="col" className={thNum}>
                    <abbr title="Time on ice" className="no-underline">
                      TOI
                    </abbr>
                  </th>
                </tr>
              </thead>
              <tbody>
                {player.games.map((g) => (
                  <tr
                    key={g.gameId}
                    className="border-b border-surface-100 dark:border-white/[0.03] hover:bg-surface-50 dark:hover:bg-white/[0.02] transition-colors"
                  >
                    <td className={`${td} font-mono text-xs`}>
                      <Link
                        to={`/game/${g.gameId}`}
                        className="inline-block py-0.5 hover:text-accent-600 dark:hover:text-accent-400 hover:underline"
                      >
                        {formatShortDate(g.gameDate, timeZone)}
                      </Link>
                    </td>
                    <td className={td}>
                      <Link
                        to={`/team/${g.opponent.id}`}
                        className="inline-flex items-center gap-2 py-0.5 hover:text-accent-600 dark:hover:text-accent-400 hover:underline"
                      >
                        <span className="w-4 text-xs text-surface-500 dark:text-surface-400">
                          <span aria-hidden="true">{g.isHome ? 'vs' : '@'}</span>
                          <span className="sr-only">{g.isHome ? 'Home against' : 'Away at'}</span>
                        </span>
                        <TeamLogo
                          src={g.opponent.logoUri}
                          alt=""
                          className="h-5 w-5 object-contain"
                        />
                        <span className="font-medium whitespace-nowrap">
                          {g.opponent.locationName} {g.opponent.teamName}
                        </span>
                      </Link>
                    </td>
                    <td className={td}>
                      <ResultBadge game={g} />
                    </td>
                    {isGoalie ? (
                      <>
                        <td className={tdNum}>{g.stats.shotsAgainst}</td>
                        <td className={tdNum}>{g.stats.saves}</td>
                        <td className={tdNum}>{g.stats.goalsAgainst}</td>
                        <td className={tdNum}>{formatSavePct(g.stats.savePct)}</td>
                      </>
                    ) : (
                      <>
                        <td className={tdNum}>{g.stats.goals}</td>
                        <td className={tdNum}>{g.stats.assists}</td>
                        <td className={tdNum}>{g.stats.points}</td>
                        <td className={tdNum}>{g.stats.shotsOnGoal}</td>
                        <td className={tdNum}>{formatPlusMinus(g.stats.plusMinus)}</td>
                      </>
                    )}
                    <td className={tdNum}>{formatToi(g.stats.timeOnIceSeconds)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </ScrollRegion>
        </div>
      ) : (
        <div className="text-center text-surface-500 dark:text-surface-400 py-12">
          No games for {name} in {seasonLabel}.
        </div>
      )}
    </div>
  );
}

export default PlayerPage;
