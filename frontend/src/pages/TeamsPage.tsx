import { useState, useMemo } from 'react';
import { SeasonSelector } from '../components/SeasonSelector';
import { TeamRow } from '../components/TeamRow';
import { LogLossChart } from '../components/LogLossChart';
import { Skeleton, StatCardSkeleton } from '../components/Skeleton';
import { useFetch } from '../hooks/useFetch';
import { getAllTeams } from '../api/teams';
import { getCurrentSeason } from '../utils/season';
import type { TeamVM } from '../types';

type SortKey = 'name' | 'points' | 'record' | 'accuracy' | 'logLoss' | 'dkAccuracy' | 'dkLogLoss';
type SortDir = 'asc' | 'desc';

export function TeamsPage() {
  const [season, setSeason] = useState(getCurrentSeason);
  const [sortKey, setSortKey] = useState<SortKey>('points');
  const [sortDir, setSortDir] = useState<SortDir>('desc');

  const { data, loading, error } = useFetch(() => getAllTeams(season), [season]);

  const allGames = useMemo(() => {
    if (!data) return [];
    return data.teams.flatMap((t) => t.gameOddsVM);
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
          cmp = points(a) - points(b);
          break;
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
      }
      return sortDir === 'asc' ? cmp : -cmp;
    });
    return teams;
  }, [data, sortKey, sortDir]);

  const hasDk = data ? data.seasonTotals.draftKingsGameCount > 0 : false;

  function handleSort(key: SortKey) {
    if (sortKey === key) {
      setSortDir((d) => (d === 'asc' ? 'desc' : 'asc'));
    } else {
      setSortKey(key);
      setSortDir(key === 'logLoss' || key === 'dkLogLoss' ? 'asc' : 'desc');
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

      {data && (
        <>
          <div className="grid grid-cols-3 gap-4 mb-4">
            <div className="glass rounded-xl p-5 text-center">
              <div className="stat-number text-3xl text-surface-900 dark:text-white">
                {data.seasonTotals.totalGameCount}
              </div>
              <div className="text-xs font-medium text-surface-400 dark:text-surface-500 mt-1 uppercase tracking-wider">
                Total Games
              </div>
            </div>
            <div className="glass rounded-xl p-5 text-center">
              <div className="stat-number text-3xl text-accent-500">
                {data.seasonTotals.totalGameCount > 0
                  ? (
                      (data.seasonTotals.totalModelAccurateGameCount /
                        data.seasonTotals.totalGameCount) *
                      100
                    ).toFixed(1)
                  : '0.0'}
                %
              </div>
              <div className="text-xs font-medium text-surface-400 dark:text-surface-500 mt-1 uppercase tracking-wider">
                Home Accuracy
              </div>
            </div>
            <div className="glass rounded-xl p-5 text-center">
              <div className="stat-number text-3xl text-surface-900 dark:text-white">
                {data.seasonTotals.modelLogLoss.toFixed(4)}
              </div>
              <div className="text-xs font-medium text-surface-400 dark:text-surface-500 mt-1 uppercase tracking-wider">
                Home Log Loss
              </div>
            </div>
          </div>
          {data.seasonTotals.draftKingsGameCount > 0 && (
            <div className="grid grid-cols-3 gap-4 mb-8">
              <div className="glass rounded-xl p-5 text-center">
                <div className="stat-number text-3xl text-surface-900 dark:text-white">
                  {data.seasonTotals.draftKingsGameCount}
                </div>
                <div className="text-xs font-medium text-surface-400 dark:text-surface-500 mt-1 uppercase tracking-wider">
                  DK Games
                </div>
              </div>
              <div className="glass rounded-xl p-5 text-center">
                <div className="stat-number text-3xl text-accent-500">
                  {(
                    (data.seasonTotals.draftKingsAccurateGameCount /
                      data.seasonTotals.draftKingsGameCount) *
                    100
                  ).toFixed(1)}
                  %
                </div>
                <div className="text-xs font-medium text-surface-400 dark:text-surface-500 mt-1 uppercase tracking-wider">
                  DK Accuracy
                </div>
              </div>
              <div className="glass rounded-xl p-5 text-center">
                <div className="stat-number text-3xl text-surface-900 dark:text-white">
                  {data.seasonTotals.draftKingsLogLoss.toFixed(4)}
                </div>
                <div className="text-xs font-medium text-surface-400 dark:text-surface-500 mt-1 uppercase tracking-wider">
                  DK Log Loss
                </div>
              </div>
            </div>
          )}
        </>
      )}

      <LogLossChart games={allGames} deduplicateById />

      {error && <div className="glass rounded-xl text-center text-red-500 py-8">{error}</div>}

      {loading && (
        <>
          <div className="grid grid-cols-3 gap-4 mb-8">
            <StatCardSkeleton />
            <StatCardSkeleton />
            <StatCardSkeleton />
          </div>
          <Skeleton className="h-[310px] w-full mb-8" />
          <div className="space-y-3">
            {Array.from({ length: 8 }).map((_, i) => (
              <Skeleton key={i} className="h-14 w-full" />
            ))}
          </div>
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
                </tr>
              </thead>
              <tbody>
                {sorted.map((team) => (
                  <TeamRow key={team.id} team={team} season={season} showDk={hasDk} />
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
