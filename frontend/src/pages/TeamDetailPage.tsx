import { useMemo, useState } from 'react';
import {
  Link,
  useLoaderData,
  useParams,
  useSearchParams,
  useNavigate,
  type LoaderFunctionArgs,
} from 'react-router';
import { useFetch } from '../hooks/useFetch';
import { getTeam } from '../api/teams';
import { getTeamRoster } from '../api/players';
import { forwardClientIp } from '../api/client';
import { formatSeasonLabel, getCurrentSeason } from '../utils/season';
import { InjuredReserveBadge } from '../components/InjuredReserveBadge';
import { formatShortDate } from '../utils/dates';
import { useDisplayTimeZone } from '../hooks/useDisplayTimeZone';
import { wasCorrectlyPredicted, calculateLogLoss } from '../utils/predictions';
import { Skeleton } from '../components/Skeleton';
import { SeasonSelector } from '../components/SeasonSelector';
import { LogLossChart } from '../components/LogLossChart';
import { CalendarSubscribe } from '../components/CalendarSubscribe';
import { Winner } from '../types';
import { useOddsFormatContext } from '../contexts/OddsFormatContext';
import { formatOdds } from '../utils/oddsFormat';
import { breadcrumbs, sportsTeam } from '../utils/structuredData';
import { LoadingStatus, PageTitle, ScrollRegion } from '../components/A11y';
import { Breadcrumbs, type Crumb } from '../components/Breadcrumbs';
import { StatCard } from '../components/StatCard';
import { playerName, playerPath, positionGroups, positionName } from '../utils/players';
import type { RosterPlayerVM } from '../types';

// The game log starts with the games around the last one played; the rest are behind a button
const LOG_GAMES_SHOWN = 10;
const LOG_UPCOMING_SHOWN = 3;

// The API lists games newest first, so upcoming games come before played ones. Picked from
// hasBeenPlayed, not the clock, so the server and the browser show the same rows.
function recentGames<T extends { hasBeenPlayed: boolean }>(games: T[]): T[] {
  const firstPlayed = games.findIndex((g) => g.hasBeenPlayed);
  const boundary = firstPlayed === -1 ? games.length : firstPlayed;
  const start = Math.max(
    0,
    Math.min(boundary - LOG_UPCOMING_SHOWN, games.length - LOG_GAMES_SHOWN),
  );
  return games.slice(start, start + LOG_GAMES_SHOWN);
}

function seasonFrom(searchParams: URLSearchParams) {
  return Number(searchParams.get('season')) || getCurrentSeason();
}

// First load renders on the server; client-side navigation fetches in the page as before
export async function loader({ params, request }: LoaderFunctionArgs) {
  const season = seasonFrom(new URL(request.url).searchParams);
  const init = forwardClientIp(request);
  const [team, roster] = await Promise.all([
    getTeam(Number(params.teamId), season, init).catch(() => null),
    getTeamRoster(Number(params.teamId), season, init).catch(() => null),
  ]);
  return { team, roster };
}

export async function clientLoader() {
  return null;
}

export function TeamDetailPage() {
  const initial = useLoaderData<typeof loader>();
  const { format } = useOddsFormatContext();
  const { teamId } = useParams();
  const { timeZone } = useDisplayTimeZone();
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();
  const season = seasonFrom(searchParams);

  const {
    data: team,
    loading,
    error,
  } = useFetch(() => getTeam(Number(teamId), season), [teamId, season], initial?.team);
  const { data: roster } = useFetch(
    () => getTeamRoster(Number(teamId), season),
    [teamId, season],
    initial?.roster,
  );

  const kalshiStats = useMemo(() => {
    if (!team) return null;
    let games = 0;
    let accurate = 0;
    let totalLoss = 0;
    for (const g of team.gameOddsVM) {
      if (!g.hasBeenPlayed) continue;
      const k = g.bookmakerOdds?.find((b) => b.bookmakerName === 'Kalshi');
      if (!k || k.homeOdds <= 0 || k.awayOdds <= 0) continue;
      games++;
      const loss = calculateLogLoss(k.homeOdds, k.awayOdds, g.winner);
      totalLoss += loss;
      const kPredictedHome = k.homeOdds > k.awayOdds;
      const homeWon = g.winner === Winner.HOME;
      if (kPredictedHome === homeWon) accurate++;
    }
    return {
      games,
      accurate,
      logLoss: games > 0 ? totalLoss / games : 0,
      accuracyPct: games > 0 ? ((accurate / games) * 100).toFixed(1) : '0.0',
    };
  }, [team]);

  // Collapses again when the team or season changes
  const logKey = `${teamId}-${season}`;
  const [expandedLog, setExpandedLog] = useState<string | null>(null);
  const showAllGames = expandedLog === logKey;
  const logGames = useMemo(() => {
    if (!team) return [];
    return showAllGames ? team.gameOddsVM : recentGames(team.gameOddsVM);
  }, [team, showAllGames]);

  if (loading) {
    return (
      <div className="space-y-4">
        <PageTitle title="Team" />
        <LoadingStatus label="Loading team…" />
        <Skeleton className="h-10 w-1/3" />
        <Skeleton className="h-64 w-full" />
      </div>
    );
  }

  // A title of their own, kept out of search, so these never leave the tab blank
  if (error)
    return (
      <>
        <PageTitle title={error === 'Team not found.' ? 'Team not found' : 'Team'} noindex />
        <div
          role="alert"
          className="glass rounded-xl text-center text-red-700 dark:text-red-400 py-8"
        >
          {error}
        </div>
      </>
    );
  if (!team)
    return (
      <div className="text-center text-surface-500 dark:text-surface-400 py-12">
        <PageTitle title="Team not found" noindex />
        Team not found.
      </div>
    );

  const accuracyPct =
    team.totalGameCount > 0
      ? ((team.totalModelAccurateGameCount / team.totalGameCount) * 100).toFixed(1)
      : '0.0';
  const dkAccuracyPct =
    team.draftKingsGameCount > 0
      ? ((team.draftKingsAccurateGameCount / team.draftKingsGameCount) * 100).toFixed(1)
      : '0.0';

  const isCurrentSeason = season === getCurrentSeason();
  const crumbs: Crumb[] = [
    { name: 'Teams', path: '/teams' },
    { name: `${team.locationName} ${team.teamName}`, path: `/team/${team.id}` },
  ];

  return (
    <div>
      <PageTitle
        title={`${team.locationName} ${team.teamName}`}
        description={`${team.locationName} ${team.teamName} games with NHL Odds model predictions, results, and betting record.`}
        // A team's athletes are its players now: past seasons' rosters aren't listed
        schema={[sportsTeam(team, isCurrentSeason ? (roster ?? []) : []), breadcrumbs(crumbs)]}
        imageAlt={`${team.locationName} ${team.teamName} logo with this season's record, how often the model picked their games right, and their next game`}
      />
      <Breadcrumbs items={crumbs} />
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between mb-8 gap-4">
        <div className="flex items-center gap-5">
          <img
            src={team.logoUri}
            alt=""
            className="h-20 w-20 object-contain drop-shadow-lg"
            onError={(e) => {
              (e.target as HTMLImageElement).style.display = 'none';
            }}
          />
          <div>
            <h1 className="font-display text-3xl font-bold tracking-tight">
              {team.locationName} {team.teamName}
            </h1>
            <span className="stat-number text-sm text-surface-500 dark:text-surface-400">
              <span className="sr-only">Record (wins, losses, overtime losses): </span>
              {team.seasonWins}-{team.seasonLosses}-{team.seasonOvertimeLosses}
            </span>
          </div>
        </div>
        <div className="flex items-center gap-2 flex-wrap">
          {/* The feed is this season's games, so it's offered only on this season's page */}
          {season === getCurrentSeason() && (
            <CalendarSubscribe
              teamId={team.id}
              teamName={`${team.locationName} ${team.teamName}`}
            />
          )}
          <SeasonSelector value={season} onChange={(s) => setSearchParams({ season: String(s) })} />
        </div>
      </div>

      <LogLossChart games={team.gameOddsVM} />

      <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-3 mb-8">
        <StatCard label="In-House Accuracy" value={`${accuracyPct}%`} accent />
        <StatCard
          label="In-House Log Loss"
          value={team.modelLogLoss.toFixed(4)}
          sub={`${team.totalGameCount} games`}
        />
        {team.draftKingsGameCount > 0 && (
          <>
            <StatCard label="DK Accuracy" value={`${dkAccuracyPct}%`} />
            <StatCard
              label="DK Log Loss"
              value={team.draftKingsLogLoss.toFixed(4)}
              sub={`${team.draftKingsGameCount} games`}
            />
          </>
        )}
        {kalshiStats && kalshiStats.games > 0 && (
          <>
            <StatCard label="Kalshi Accuracy" value={`${kalshiStats.accuracyPct}%`} />
            <StatCard
              label="Kalshi Log Loss"
              value={kalshiStats.logLoss.toFixed(4)}
              sub={`${kalshiStats.games} games`}
            />
          </>
        )}
      </div>

      {team.gameOddsVM.length > 0 && (
        <div className="glass rounded-xl overflow-hidden">
          <ScrollRegion label="Game log">
            <table className="w-full text-sm">
              <caption className="sr-only">
                Game log. Rows are tinted blue when the in-house model picked the winner and red
                when it missed.
              </caption>
              <thead>
                <tr className="border-b border-surface-200 dark:border-white/[0.06] text-left text-xs uppercase tracking-wider text-surface-500 dark:text-surface-400">
                  <th scope="col" className="py-3 px-4 font-semibold">
                    Date
                  </th>
                  <th scope="col" className="py-3 px-4 font-semibold">
                    Opponent
                  </th>
                  <th scope="col" className="py-3 px-4 text-center font-semibold">
                    <abbr title="Home or away" className="no-underline">
                      H/A
                    </abbr>
                  </th>
                  <th scope="col" className="py-3 px-4 text-center font-semibold">
                    Home Odds
                  </th>
                  <th scope="col" className="py-3 px-4 text-center font-semibold">
                    DK Odds
                  </th>
                  <th scope="col" className="py-3 px-4 text-center font-semibold">
                    Kalshi Odds
                  </th>
                  <th scope="col" className="py-3 px-4 text-center font-semibold">
                    Score
                  </th>
                  <th scope="col" className="py-3 px-4 text-center font-semibold">
                    Result
                  </th>
                  <th scope="col" className="py-3 px-4 text-center font-semibold">
                    Home Loss
                  </th>
                  <th scope="col" className="py-3 px-4 text-center font-semibold">
                    DK Loss
                  </th>
                  <th scope="col" className="py-3 px-4 text-center font-semibold">
                    Kalshi Loss
                  </th>
                </tr>
              </thead>
              <tbody>
                {logGames.map((game) => {
                  const isHome = game.homeTeam?.id === team.id;
                  const opponent = isHome ? game.awayTeam : game.homeTeam;
                  const teamSide = isHome ? game.homeTeam : game.awayTeam;
                  const dk = game.bookmakerOdds?.find((b) => b.bookmakerName === 'DraftKings');
                  const kalshi = game.bookmakerOdds?.find((b) => b.bookmakerName === 'Kalshi');
                  const dkTeamOdds = dk ? (isHome ? dk.homeOdds : dk.awayOdds) : null;
                  const kalshiTeamOdds = kalshi
                    ? isHome
                      ? kalshi.homeOdds
                      : kalshi.awayOdds
                    : null;
                  const correct = game.hasBeenPlayed && wasCorrectlyPredicted(game);
                  const incorrect = game.hasBeenPlayed && !wasCorrectlyPredicted(game);
                  const won =
                    game.hasBeenPlayed &&
                    ((isHome && game.winner === Winner.HOME) ||
                      (!isHome && game.winner === Winner.AWAY));

                  return (
                    <tr
                      key={game.id}
                      onClick={(e) => {
                        if (!(e.target as HTMLElement).closest('a'))
                          navigate(`/game/${game.id}`, { state: { game } });
                      }}
                      className={`border-b border-surface-100 dark:border-white/[0.03] transition-colors cursor-pointer ${
                        correct
                          ? 'bg-blue-50/50 dark:bg-blue-500/[0.04] hover:bg-blue-50 dark:hover:bg-blue-500/[0.07]'
                          : incorrect
                            ? 'bg-red-50/50 dark:bg-red-500/[0.04] hover:bg-red-50 dark:hover:bg-red-500/[0.07]'
                            : 'hover:bg-surface-50 dark:hover:bg-white/[0.02]'
                      }`}
                    >
                      <td className="py-3 px-4 font-mono text-xs">
                        <Link
                          to={`/game/${game.id}`}
                          state={{ game }}
                          className="hover:text-accent-600 dark:hover:text-accent-400 hover:underline"
                        >
                          {formatShortDate(game.gameDate, timeZone)}
                        </Link>
                      </td>
                      <td className="py-3 px-4">
                        <div className="flex items-center gap-2.5">
                          {opponent && (
                            <img
                              src={opponent.logoUri}
                              alt=""
                              className="h-5 w-5 object-contain"
                              onError={(e) => {
                                (e.target as HTMLImageElement).style.display = 'none';
                              }}
                            />
                          )}
                          <span className="font-medium">
                            {opponent ? `${opponent.locationName} ${opponent.teamName}` : 'TBD'}
                          </span>
                        </div>
                      </td>
                      <td className="py-3 px-4 text-center">
                        <span
                          className={`inline-block px-2 py-0.5 rounded text-xs font-mono font-semibold ${
                            isHome
                              ? 'bg-accent-500/10 text-accent-600 dark:text-accent-400'
                              : 'bg-surface-200/50 dark:bg-white/[0.05] text-surface-500 dark:text-surface-400'
                          }`}
                        >
                          <span aria-hidden="true">{isHome ? 'H' : 'A'}</span>
                          <span className="sr-only">{isHome ? 'Home' : 'Away'}</span>
                        </span>
                      </td>
                      <td className="py-3 px-4 text-center stat-number text-accent-600 dark:text-accent-400">
                        {teamSide ? formatOdds(teamSide.modelOdds, format) : '-'}
                        {game.hasBeenPlayed && (
                          <>
                            <span aria-hidden="true" className="ml-1">
                              {correct ? '\u2713' : '\u2717'}
                            </span>
                            <span className="sr-only">
                              {correct ? ' (correct pick)' : ' (missed pick)'}
                            </span>
                          </>
                        )}
                      </td>
                      <td className="py-3 px-4 text-center stat-number">
                        {dkTeamOdds != null ? formatOdds(dkTeamOdds, format) : '-'}
                      </td>
                      <td className="py-3 px-4 text-center stat-number">
                        {kalshiTeamOdds != null ? formatOdds(kalshiTeamOdds, format) : '-'}
                      </td>
                      <td className="py-3 px-4 text-center stat-number">
                        {game.hasBeenPlayed
                          ? `${game.awayTeam?.goals ?? 0}-${game.homeTeam?.goals ?? 0}`
                          : '-'}
                      </td>
                      <td className="py-3 px-4 text-center">
                        {game.hasBeenPlayed ? (
                          <span
                            className={`inline-block px-2 py-0.5 rounded text-xs font-bold ${
                              won
                                ? 'bg-blue-500/10 text-blue-700 dark:text-blue-400'
                                : 'bg-red-500/10 text-red-700 dark:text-red-400'
                            }`}
                          >
                            <span aria-hidden="true">{won ? 'W' : 'L'}</span>
                            <span className="sr-only">{won ? 'Win' : 'Loss'}</span>
                          </span>
                        ) : (
                          <span className="text-surface-500 dark:text-surface-400">-</span>
                        )}
                      </td>
                      <td className="py-3 px-4 text-center stat-number text-xs">
                        {game.hasBeenPlayed && game.logLoss != null ? game.logLoss.toFixed(4) : '-'}
                      </td>
                      <td className="py-3 px-4 text-center stat-number text-xs">
                        {game.hasBeenPlayed && dk
                          ? calculateLogLoss(dk.homeOdds, dk.awayOdds, game.winner).toFixed(4)
                          : '-'}
                      </td>
                      <td className="py-3 px-4 text-center stat-number text-xs">
                        {game.hasBeenPlayed && kalshi && kalshi.homeOdds > 0 && kalshi.awayOdds > 0
                          ? calculateLogLoss(kalshi.homeOdds, kalshi.awayOdds, game.winner).toFixed(
                              4,
                            )
                          : '-'}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </ScrollRegion>
          {team.gameOddsVM.length > LOG_GAMES_SHOWN && (
            <div className="border-t border-surface-200 dark:border-white/[0.06] px-4 py-3 text-center">
              <button
                type="button"
                aria-expanded={showAllGames}
                onClick={() => setExpandedLog(showAllGames ? null : logKey)}
                className="text-sm font-medium text-accent-700 dark:text-accent-400 hover:underline"
              >
                {showAllGames ? 'Show recent games' : `Show all ${team.gameOddsVM.length} games`}
              </button>
            </div>
          )}
        </div>
      )}

      {team.gameOddsVM.length === 0 && (
        <div className="text-center text-surface-500 dark:text-surface-400 py-12">
          No games found for this team and season.
        </div>
      )}

      {roster && roster.length > 0 && (
        <Roster
          players={roster}
          heading={
            isCurrentSeason
              ? 'Roster'
              : `Played for the ${team.teamName} in ${formatSeasonLabel(season)}`
          }
        />
      )}
    </div>
  );
}

// Each player linked to their page. This season: the NHL's latest roster report
// for the team (injured reserve marked); past seasons: the players whose latest
// game that season was for the team
function Roster({ players, heading }: { players: RosterPlayerVM[]; heading: string }) {
  return (
    <section aria-labelledby="roster-heading" className="glass rounded-xl p-4 sm:p-5 mt-8">
      <h2
        id="roster-heading"
        className="text-xs font-mono font-bold tracking-widest uppercase text-surface-500 dark:text-surface-400 mb-4"
      >
        {heading}
      </h2>
      <div className="space-y-5">
        {positionGroups(players).map((group) => (
          <div key={group.label}>
            <h3 className="text-sm font-semibold mb-2">{group.label}</h3>
            <ul className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-x-4 gap-y-1">
              {group.players.map((p) => (
                <li key={p.id}>
                  <Link
                    to={playerPath(p.id)}
                    className="flex items-center gap-3 rounded-lg px-2 py-1.5 hover:bg-surface-100 dark:hover:bg-white/[0.04] group"
                  >
                    <img
                      src={p.headShot}
                      alt=""
                      loading="lazy"
                      className="h-9 w-9 shrink-0 rounded-full object-cover bg-surface-200/60 dark:bg-white/[0.06]"
                      onError={(e) => {
                        (e.target as HTMLImageElement).style.visibility = 'hidden';
                      }}
                    />
                    <span className="stat-number w-7 text-right text-xs text-surface-500 dark:text-surface-400">
                      {p.sweaterNumber != null ? `#${p.sweaterNumber}` : ''}
                    </span>
                    <span className="min-w-0 flex-1">
                      <span className="block truncate font-medium group-hover:text-accent-600 dark:group-hover:text-accent-400 group-hover:underline">
                        {playerName(p)}
                      </span>
                      <span className="block text-xs text-surface-500 dark:text-surface-400">
                        {positionName(p.position)} · {p.gamesPlayed}{' '}
                        {p.gamesPlayed === 1 ? 'game' : 'games'}
                        {p.isInjuredReserve && (
                          <>
                            {' '}
                            <InjuredReserveBadge short />
                          </>
                        )}
                      </span>
                    </span>
                  </Link>
                </li>
              ))}
            </ul>
          </div>
        ))}
      </div>
    </section>
  );
}

export default TeamDetailPage;
