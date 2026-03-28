import { useState, useMemo } from 'react';
import { SeasonSelector } from '../components/SeasonSelector';
import { TeamRow } from '../components/TeamRow';
import { LogLossChart } from '../components/LogLossChart';
import { Skeleton, StatCardSkeleton } from '../components/Skeleton';
import { useFetch } from '../hooks/useFetch';
import { getAllTeams } from '../api/teams';
import { getCurrentSeason } from '../utils/season';
import { calculateLogLoss } from '../utils/predictions';
import { Winner } from '../types';
import type { TeamVM } from '../types';

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

export function TeamsPage() {
  const [season, setSeason] = useState(getCurrentSeason);
  const [sortKey, setSortKey] = useState<SortKey>('points');
  const [sortDir, setSortDir] = useState<SortDir>('desc');

  const { data, loading, error } = useFetch(() => getAllTeams(season), [season]);

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

  const arrow = (key: SortKey) =>
    sortKey === key ? (sortDir === 'asc' ? ' \u25B2' : ' \u25BC') : '';

  return (
    <div>
      <div className="flex items-center justify-between mb-8">
        <h1 className="font-display text-3xl font-bold tracking-tight">Teams</h1>
        <SeasonSelector value={season} onChange={setSeason} />
      </div>

      {error && <div className="glass rounded-xl text-center text-red-500 py-8">{error}</div>}

      {loading && (
        <>
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
              <div className="text-[10px] sm:text-xs font-medium text-surface-400 dark:text-surface-500 mt-1 uppercase tracking-wider">
                Total Games
              </div>
            </div>
            <div className="glass rounded-xl p-4 sm:p-5 text-center">
              <div className="stat-number text-2xl sm:text-3xl text-accent-500">
                {data.seasonTotals.totalGameCount > 0
                  ? (
                      (data.seasonTotals.totalModelAccurateGameCount /
                        data.seasonTotals.totalGameCount) *
                      100
                    ).toFixed(1)
                  : '0.0'}
                %
              </div>
              <div className="text-[10px] sm:text-xs font-medium text-surface-400 dark:text-surface-500 mt-1 uppercase tracking-wider">
                Home Acc
              </div>
            </div>
            <div className="glass rounded-xl p-4 sm:p-5 text-center">
              <div className="stat-number text-2xl sm:text-3xl text-surface-900 dark:text-white">
                {data.seasonTotals.modelLogLoss.toFixed(4)}
              </div>
              <div className="text-[10px] sm:text-xs font-medium text-surface-400 dark:text-surface-500 mt-1 uppercase tracking-wider">
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
                <div className="text-[10px] sm:text-xs font-medium text-surface-400 dark:text-surface-500 mt-1 uppercase tracking-wider">
                  DK Games
                </div>
              </div>
              <div className="glass rounded-xl p-4 sm:p-5 text-center">
                <div className="stat-number text-2xl sm:text-3xl text-accent-500">
                  {(
                    (data.seasonTotals.draftKingsAccurateGameCount /
                      data.seasonTotals.draftKingsGameCount) *
                    100
                  ).toFixed(1)}
                  %
                </div>
                <div className="text-[10px] sm:text-xs font-medium text-surface-400 dark:text-surface-500 mt-1 uppercase tracking-wider">
                  DK Acc
                </div>
              </div>
              <div className="glass rounded-xl p-4 sm:p-5 text-center">
                <div className="stat-number text-2xl sm:text-3xl text-surface-900 dark:text-white">
                  {data.seasonTotals.draftKingsLogLoss.toFixed(4)}
                </div>
                <div className="text-[10px] sm:text-xs font-medium text-surface-400 dark:text-surface-500 mt-1 uppercase tracking-wider">
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
                <div className="text-[10px] sm:text-xs font-medium text-surface-400 dark:text-surface-500 mt-1 uppercase tracking-wider">
                  Kalshi Games
                </div>
              </div>
              <div className="glass rounded-xl p-4 sm:p-5 text-center">
                <div className="stat-number text-2xl sm:text-3xl text-accent-500">
                  {((kalshiSeasonTotals.accurate / kalshiSeasonTotals.games) * 100).toFixed(1)}%
                </div>
                <div className="text-[10px] sm:text-xs font-medium text-surface-400 dark:text-surface-500 mt-1 uppercase tracking-wider">
                  Kalshi Acc
                </div>
              </div>
              <div className="glass rounded-xl p-4 sm:p-5 text-center">
                <div className="stat-number text-2xl sm:text-3xl text-surface-900 dark:text-white">
                  {kalshiSeasonTotals.logLoss.toFixed(4)}
                </div>
                <div className="text-[10px] sm:text-xs font-medium text-surface-400 dark:text-surface-500 mt-1 uppercase tracking-wider">
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
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-surface-200 dark:border-white/[0.06] text-left text-xs uppercase tracking-wider text-surface-400 dark:text-surface-500">
                  <th
                    className="py-3 px-4 cursor-pointer select-none hover:text-surface-900 dark:hover:text-white transition-colors font-semibold"
                    onClick={() => handleSort('name')}
                  >
                    Team{arrow('name')}
                  </th>
                  <th
                    className="py-3 px-4 text-center cursor-pointer select-none hover:text-surface-900 dark:hover:text-white transition-colors font-semibold"
                    onClick={() => handleSort('points')}
                  >
                    Pts{arrow('points')}
                  </th>
                  <th
                    className="py-3 px-4 text-center cursor-pointer select-none hover:text-surface-900 dark:hover:text-white transition-colors font-semibold"
                    onClick={() => handleSort('record')}
                  >
                    Record{arrow('record')}
                  </th>
                  <th
                    className="py-3 px-4 text-center cursor-pointer select-none hover:text-surface-900 dark:hover:text-white transition-colors font-semibold"
                    onClick={() => handleSort('accuracy')}
                  >
                    Home Acc{arrow('accuracy')}
                  </th>
                  <th
                    className="py-3 px-4 text-center cursor-pointer select-none hover:text-surface-900 dark:hover:text-white transition-colors font-semibold"
                    onClick={() => handleSort('logLoss')}
                  >
                    Home Loss{arrow('logLoss')}
                  </th>
                  {hasDk && (
                    <th
                      className="py-3 px-4 text-center cursor-pointer select-none hover:text-surface-900 dark:hover:text-white transition-colors font-semibold"
                      onClick={() => handleSort('dkAccuracy')}
                    >
                      DK Acc{arrow('dkAccuracy')}
                    </th>
                  )}
                  {hasDk && (
                    <th
                      className="py-3 px-4 text-center cursor-pointer select-none hover:text-surface-900 dark:hover:text-white transition-colors font-semibold"
                      onClick={() => handleSort('dkLogLoss')}
                    >
                      DK Loss{arrow('dkLogLoss')}
                    </th>
                  )}
                  {hasKalshi && (
                    <th
                      className="py-3 px-4 text-center cursor-pointer select-none hover:text-surface-900 dark:hover:text-white transition-colors font-semibold"
                      onClick={() => handleSort('kalshiAccuracy')}
                    >
                      Kalshi Acc{arrow('kalshiAccuracy')}
                    </th>
                  )}
                  {hasKalshi && (
                    <th
                      className="py-3 px-4 text-center cursor-pointer select-none hover:text-surface-900 dark:hover:text-white transition-colors font-semibold"
                      onClick={() => handleSort('kalshiLogLoss')}
                    >
                      Kalshi Loss{arrow('kalshiLogLoss')}
                    </th>
                  )}
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
          </div>
        </div>
      )}

      {!loading && !error && sorted.length === 0 && (
        <div className="text-center text-surface-400 dark:text-surface-500 py-12">
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
