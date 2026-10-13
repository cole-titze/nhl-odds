import { useState, useMemo } from 'react';
import { useLoaderData, type LoaderFunctionArgs } from 'react-router';
import { SeasonSelector } from '../components/SeasonSelector';
import { TeamRow } from '../components/TeamRow';
import { LogLossChart } from '../components/LogLossChart';
import { Skeleton, StatCardSkeleton } from '../components/Skeleton';
import { LoadingStatus, PageTitle, ScrollRegion } from '../components/A11y';
import { useFetch } from '../hooks/useFetch';
import { getAllTeams } from '../api/teams';
import { forwardClientIp } from '../api/client';
import { getCurrentSeason } from '../utils/season';
import { calculateLogLoss } from '../utils/predictions';
import { Winner } from '../types';
import type { BookmakerOddsVM, GameOddsVM, TeamsVM, TeamVM } from '../types';

type SortKey =
  | 'name'
  | 'points'
  | 'record'
  | 'accuracy'
  | 'logLoss'
  | 'dkAccuracy'
  | 'dkLogLoss'
  | 'kalshiAccuracy'
  | 'kalshiLogLoss';
type SortDir = 'asc' | 'desc';

interface KalshiTeamStats {
  games: number;
  accurate: number;
  logLoss: number;
  accuracyPct: string;
}

function computeKalshiStats(team: TeamVM): KalshiTeamStats {
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
}

// The fields of each game this page reads (season chart, Kalshi stats)
type TeamsPageGame = Pick<
  GameOddsVM,
  'id' | 'gameDate' | 'hasBeenPlayed' | 'logLoss' | 'winner'
> & {
  bookmakerOdds: Pick<BookmakerOddsVM, 'bookmakerName' | 'homeOdds' | 'awayOdds'>[];
};

interface CompactTeams {
  seasonTotals: TeamsVM['seasonTotals'];
  teams: (Omit<TeamVM, 'gameOddsVM'> & { gameIds: number[] })[];
  games: TeamsPageGame[];
}

// The full response lists every game twice (once per team) with every field,
// which made the server's HTML ~8 MB late in a season. The page gets each game
// once with only the fields it reads (~1.6 MB).
function compactTeams({ teams, seasonTotals }: TeamsVM): CompactTeams {
  const games = new Map<number, TeamsPageGame>();
  for (const t of teams) {
    for (const g of t.gameOddsVM) {
      games.set(g.id, {
        id: g.id,
        gameDate: g.gameDate,
        hasBeenPlayed: g.hasBeenPlayed,
        logLoss: g.logLoss,
        winner: g.winner,
        bookmakerOdds: (g.bookmakerOdds ?? []).map(({ bookmakerName, homeOdds, awayOdds }) => ({
          bookmakerName,
          homeOdds,
          awayOdds,
        })),
      });
    }
  }
  return {
    seasonTotals,
    teams: teams.map(({ gameOddsVM, ...team }) => ({
      ...team,
      gameIds: gameOddsVM.map((g) => g.id),
    })),
    games: [...games.values()],
  };
}

function expandTeams({ teams, seasonTotals, games }: CompactTeams): TeamsVM {
  const byId = new Map(games.map((g) => [g.id, g]));
  return {
    seasonTotals,
    teams: teams.map(({ gameIds, ...team }) => ({
      ...team,
      // Only the TeamsPageGame fields are read on this page
      gameOddsVM: gameIds.map((id) => byId.get(id)!) as unknown as GameOddsVM[],
    })),
  };
}

// First load renders on the server with this season's data. Client-side
// navigation skips it (clientLoader) and the page fetches as before.
export async function loader({ request }: LoaderFunctionArgs) {
  return getAllTeams(getCurrentSeason(), forwardClientIp(request)).then(compactTeams, () => null);
}

export async function clientLoader() {
  return null;
}

export function TeamsPage() {
  const loaded = useLoaderData<typeof loader>();
  const [initial] = useState(() => (loaded ? expandTeams(loaded) : null));
  const [season, setSeason] = useState(getCurrentSeason);
  const [sortKey, setSortKey] = useState<SortKey>('points');
  const [sortDir, setSortDir] = useState<SortDir>('desc');

  const { data, loading, error } = useFetch(() => getAllTeams(season), [season], initial);

  const allGames = useMemo(() => {
    if (!data) return [];
    return data.teams.flatMap((t) => t.gameOddsVM);
  }, [data]);

  const kalshiByTeamId = useMemo(() => {
    if (!data) return new Map<number, KalshiTeamStats>();
    const map = new Map<number, KalshiTeamStats>();
    for (const t of data.teams) {
      map.set(t.id, computeKalshiStats(t));
    }
    return map;
  }, [data]);

  const kalshiSeasonTotals = useMemo(() => {
    if (!data) return { games: 0, accurate: 0, logLoss: 0 };
    let games = 0;
    let accurate = 0;
    let totalLoss = 0;
    const seen = new Set<number>();
    for (const t of data.teams) {
      for (const g of t.gameOddsVM) {
        if (!g.hasBeenPlayed || seen.has(g.id)) continue;
        seen.add(g.id);
        const k = g.bookmakerOdds?.find((b) => b.bookmakerName === 'Kalshi');
        if (!k || k.homeOdds <= 0 || k.awayOdds <= 0) continue;
        games++;
        totalLoss += calculateLogLoss(k.homeOdds, k.awayOdds, g.winner);
        const kPredictedHome = k.homeOdds > k.awayOdds;
        const homeWon = g.winner === Winner.HOME;
        if (kPredictedHome === homeWon) accurate++;
      }
    }
    return { games, accurate, logLoss: games > 0 ? totalLoss / games : 0 };
  }, [data]);

  const sorted = useMemo(() => {
    if (!data) return [];
    const teams = [...data.teams];
    teams.sort((a, b) => {
      let cmp = 0;
      switch (sortKey) {
        case 'name':
          cmp = `${a.locationName} ${a.teamName}`.localeCompare(`${b.locationName} ${b.teamName}`);
          break;
        case 'points':
        case 'record':
          cmp = points(a) - points(b);
          break;
        case 'accuracy':
          cmp = accuracy(a) - accuracy(b);
          break;
        case 'logLoss':
          cmp = a.modelLogLoss - b.modelLogLoss;
          break;
        case 'dkAccuracy':
          cmp = dkAccuracy(a) - dkAccuracy(b);
          break;
        case 'dkLogLoss':
          cmp = a.draftKingsLogLoss - b.draftKingsLogLoss;
          break;
        case 'kalshiAccuracy': {
          const ka = kalshiByTeamId.get(a.id);
          const kb = kalshiByTeamId.get(b.id);
          cmp =
            (ka && ka.games > 0 ? ka.accurate / ka.games : 0) -
            (kb && kb.games > 0 ? kb.accurate / kb.games : 0);
          break;
        }
        case 'kalshiLogLoss': {
          const ka = kalshiByTeamId.get(a.id);
          const kb = kalshiByTeamId.get(b.id);
          cmp = (ka?.logLoss ?? 0) - (kb?.logLoss ?? 0);
          break;
        }
      }
      return sortDir === 'asc' ? cmp : -cmp;
    });
    return teams;
  }, [data, sortKey, sortDir, kalshiByTeamId]);

  const hasDk = data ? data.seasonTotals.draftKingsGameCount > 0 : false;
  const hasKalshi = kalshiSeasonTotals.games > 0;

  function handleSort(key: SortKey) {
    if (sortKey === key) {
      setSortDir((d) => (d === 'asc' ? 'desc' : 'asc'));
    } else {
      setSortKey(key);
      setSortDir(
        key === 'logLoss' || key === 'dkLogLoss' || key === 'kalshiLogLoss' ? 'asc' : 'desc',
      );
    }
  }

  const sortHeader = (key: SortKey, label: string, align: 'left' | 'center' = 'center') => {
    const active = sortKey === key;
    return (
      <th
        scope="col"
        aria-sort={active ? (sortDir === 'asc' ? 'ascending' : 'descending') : 'none'}
        className={`py-3 px-4 font-semibold ${align === 'center' ? 'text-center' : ''}`}
      >
        <button
          type="button"
          onClick={() => handleSort(key)}
          className="uppercase tracking-wider font-semibold cursor-pointer select-none hover:text-surface-900 dark:hover:text-white transition-colors"
        >
          {label}
          {active && <span aria-hidden="true">{sortDir === 'asc' ? ' \u25B2' : ' \u25BC'}</span>}
        </button>
      </th>
    );
  };

  return (
    <div>
      <PageTitle
        title="Teams"
        description="How the NHL Odds model has done for every NHL team this season: accuracy, log loss, and betting results."
      />
      <div className="flex items-center justify-between mb-8">
        <h1 className="font-display text-3xl font-bold tracking-tight">Teams</h1>
        <SeasonSelector value={season} onChange={setSeason} />
      </div>

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
          <LoadingStatus label="Loading teams…" />
          <Skeleton className="h-[310px] w-full mb-4 rounded-xl" />
          <div className="grid grid-cols-2 sm:grid-cols-3 gap-3 sm:gap-4 mb-4">
            <StatCardSkeleton />
            <StatCardSkeleton />
            <StatCardSkeleton />
          </div>
          <div className="grid grid-cols-2 sm:grid-cols-3 gap-3 sm:gap-4 mb-8">
            <StatCardSkeleton />
            <StatCardSkeleton />
            <StatCardSkeleton />
          </div>
          <div className="space-y-3">
            {Array.from({ length: 8 }).map((_, i) => (
              <Skeleton key={i} className="h-14 w-full rounded-xl" />
            ))}
          </div>
        </>
      )}

      <LogLossChart games={allGames} deduplicateById />

      {data && (
        <>
          <div className="grid grid-cols-2 sm:grid-cols-3 gap-3 sm:gap-4 mb-4">
            <div className="glass rounded-xl p-4 sm:p-5 text-center">
              <div className="stat-number text-2xl sm:text-3xl text-surface-900 dark:text-white">
                {data.seasonTotals.totalGameCount}
              </div>
              <div className="text-[10px] sm:text-xs font-medium text-surface-500 dark:text-surface-400 mt-1 uppercase tracking-wider">
                Total Games
              </div>
            </div>
            <div className="glass rounded-xl p-4 sm:p-5 text-center">
              <div className="stat-number text-2xl sm:text-3xl text-accent-600 dark:text-accent-400">
                {data.seasonTotals.totalGameCount > 0
                  ? (
                      (data.seasonTotals.totalModelAccurateGameCount /
                        data.seasonTotals.totalGameCount) *
                      100
                    ).toFixed(1)
                  : '0.0'}
                %
              </div>
              <div className="text-[10px] sm:text-xs font-medium text-surface-500 dark:text-surface-400 mt-1 uppercase tracking-wider">
                Home Acc
              </div>
            </div>
            <div className="glass rounded-xl p-4 sm:p-5 text-center">
              <div className="stat-number text-2xl sm:text-3xl text-surface-900 dark:text-white">
                {data.seasonTotals.modelLogLoss.toFixed(4)}
              </div>
              <div className="text-[10px] sm:text-xs font-medium text-surface-500 dark:text-surface-400 mt-1 uppercase tracking-wider">
                Home Loss
              </div>
            </div>
          </div>
          {hasDk && (
            <div className="grid grid-cols-2 sm:grid-cols-3 gap-3 sm:gap-4 mb-4">
              <div className="glass rounded-xl p-4 sm:p-5 text-center">
                <div className="stat-number text-2xl sm:text-3xl text-surface-900 dark:text-white">
                  {data.seasonTotals.draftKingsGameCount}
                </div>
                <div className="text-[10px] sm:text-xs font-medium text-surface-500 dark:text-surface-400 mt-1 uppercase tracking-wider">
                  DK Games
                </div>
              </div>
              <div className="glass rounded-xl p-4 sm:p-5 text-center">
                <div className="stat-number text-2xl sm:text-3xl text-accent-600 dark:text-accent-400">
                  {(
                    (data.seasonTotals.draftKingsAccurateGameCount /
                      data.seasonTotals.draftKingsGameCount) *
                    100
                  ).toFixed(1)}
                  %
                </div>
                <div className="text-[10px] sm:text-xs font-medium text-surface-500 dark:text-surface-400 mt-1 uppercase tracking-wider">
                  DK Acc
                </div>
              </div>
              <div className="glass rounded-xl p-4 sm:p-5 text-center">
                <div className="stat-number text-2xl sm:text-3xl text-surface-900 dark:text-white">
                  {data.seasonTotals.draftKingsLogLoss.toFixed(4)}
                </div>
                <div className="text-[10px] sm:text-xs font-medium text-surface-500 dark:text-surface-400 mt-1 uppercase tracking-wider">
                  DK Loss
                </div>
              </div>
            </div>
          )}
          {hasKalshi && (
            <div className="grid grid-cols-2 sm:grid-cols-3 gap-3 sm:gap-4 mb-8">
              <div className="glass rounded-xl p-4 sm:p-5 text-center">
                <div className="stat-number text-2xl sm:text-3xl text-surface-900 dark:text-white">
                  {kalshiSeasonTotals.games}
                </div>
                <div className="text-[10px] sm:text-xs font-medium text-surface-500 dark:text-surface-400 mt-1 uppercase tracking-wider">
                  Kalshi Games
                </div>
              </div>
              <div className="glass rounded-xl p-4 sm:p-5 text-center">
                <div className="stat-number text-2xl sm:text-3xl text-accent-600 dark:text-accent-400">
                  {((kalshiSeasonTotals.accurate / kalshiSeasonTotals.games) * 100).toFixed(1)}%
                </div>
                <div className="text-[10px] sm:text-xs font-medium text-surface-500 dark:text-surface-400 mt-1 uppercase tracking-wider">
                  Kalshi Acc
                </div>
              </div>
              <div className="glass rounded-xl p-4 sm:p-5 text-center">
                <div className="stat-number text-2xl sm:text-3xl text-surface-900 dark:text-white">
                  {kalshiSeasonTotals.logLoss.toFixed(4)}
                </div>
                <div className="text-[10px] sm:text-xs font-medium text-surface-500 dark:text-surface-400 mt-1 uppercase tracking-wider">
                  Kalshi Loss
                </div>
              </div>
            </div>
          )}
          {!hasKalshi && <div className="mb-4" />}
        </>
      )}

      {!loading && sorted.length > 0 && (
        <div className="glass rounded-xl overflow-hidden">
          <ScrollRegion label="Team standings">
            <table className="w-full text-sm">
              <caption className="sr-only">
                Team standings and prediction accuracy. Column headers are sortable.
              </caption>
              <thead>
                <tr className="border-b border-surface-200 dark:border-white/[0.06] text-left text-xs uppercase tracking-wider text-surface-500 dark:text-surface-400">
                  {sortHeader('name', 'Team', 'left')}
                  {sortHeader('points', 'Pts')}
                  {sortHeader('record', 'Record')}
                  {sortHeader('accuracy', 'Home Acc')}
                  {sortHeader('logLoss', 'Home Loss')}
                  {hasDk && sortHeader('dkAccuracy', 'DK Acc')}
                  {hasDk && sortHeader('dkLogLoss', 'DK Loss')}
                  {hasKalshi && sortHeader('kalshiAccuracy', 'Kalshi Acc')}
                  {hasKalshi && sortHeader('kalshiLogLoss', 'Kalshi Loss')}
                </tr>
              </thead>
              <tbody>
                {sorted.map((team) => (
                  <TeamRow
                    key={team.id}
                    team={team}
                    season={season}
                    showDk={hasDk}
                    showKalshi={hasKalshi}
                    kalshiStats={kalshiByTeamId.get(team.id)}
                  />
                ))}
              </tbody>
            </table>
          </ScrollRegion>
        </div>
      )}

      {!loading && !error && sorted.length === 0 && (
        <div className="text-center text-surface-500 dark:text-surface-400 py-12">
          No team data available for this season.
        </div>
      )}
    </div>
  );
}

function accuracy(t: TeamVM): number {
  return t.totalGameCount > 0 ? t.totalModelAccurateGameCount / t.totalGameCount : 0;
}

function dkAccuracy(t: TeamVM): number {
  return t.draftKingsGameCount > 0 ? t.draftKingsAccurateGameCount / t.draftKingsGameCount : 0;
}

function points(t: TeamVM): number {
  return t.seasonWins * 2 + t.seasonOvertimeLosses;
}

export default TeamsPage;
