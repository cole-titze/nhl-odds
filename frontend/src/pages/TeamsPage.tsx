import { useState, useMemo } from 'react';
import { SeasonSelector } from '../components/SeasonSelector';
import { TeamRow } from '../components/TeamRow';
import { Skeleton } from '../components/Skeleton';
import { useFetch } from '../hooks/useFetch';
import { getAllTeams } from '../api/teams';
import { getCurrentSeason } from '../utils/season';
import type { TeamVM } from '../types';

type SortKey = 'name' | 'record' | 'accuracy' | 'logLoss';
type SortDir = 'asc' | 'desc';

export function TeamsPage() {
  const [season, setSeason] = useState(getCurrentSeason);
  const [sortKey, setSortKey] = useState<SortKey>('accuracy');
  const [sortDir, setSortDir] = useState<SortDir>('desc');

  const { data, loading, error } = useFetch(() => getAllTeams(season), [season]);

  const sorted = useMemo(() => {
    if (!data) return [];
    const teams = [...data.teams];
    teams.sort((a, b) => {
      let cmp = 0;
      switch (sortKey) {
        case 'name':
          cmp = `${a.locationName} ${a.teamName}`.localeCompare(`${b.locationName} ${b.teamName}`);
          break;
        case 'record':
          cmp = a.seasonWins - a.seasonLosses - (b.seasonWins - b.seasonLosses);
          break;
        case 'accuracy':
          cmp = accuracy(a) - accuracy(b);
          break;
        case 'logLoss':
          cmp = a.modelLogLoss - b.modelLogLoss;
          break;
      }
      return sortDir === 'asc' ? cmp : -cmp;
    });
    return teams;
  }, [data, sortKey, sortDir]);

  function handleSort(key: SortKey) {
    if (sortKey === key) {
      setSortDir((d) => (d === 'asc' ? 'desc' : 'asc'));
    } else {
      setSortKey(key);
      setSortDir(key === 'logLoss' ? 'asc' : 'desc');
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
        <div className="grid grid-cols-3 gap-4 mb-8">
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
              Model Accuracy
            </div>
          </div>
          <div className="glass rounded-xl p-5 text-center">
            <div className="stat-number text-3xl text-surface-900 dark:text-white">
              {data.seasonTotals.modelLogLoss.toFixed(4)}
            </div>
            <div className="text-xs font-medium text-surface-400 dark:text-surface-500 mt-1 uppercase tracking-wider">
              Avg Log Loss
            </div>
          </div>
        </div>
      )}

      {error && <div className="glass rounded-xl text-center text-red-500 py-8">{error}</div>}

      {loading && (
        <div className="space-y-3">
          {Array.from({ length: 8 }).map((_, i) => (
            <Skeleton key={i} className="h-14 w-full" />
          ))}
        </div>
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
                    onClick={() => handleSort('record')}
                  >
                    Record{arrow('record')}
                  </th>
                  <th
                    className="py-3 px-4 text-center cursor-pointer select-none hover:text-surface-900 dark:hover:text-white transition-colors font-semibold"
                    onClick={() => handleSort('accuracy')}
                  >
                    Accuracy{arrow('accuracy')}
                  </th>
                  <th
                    className="py-3 px-4 text-center cursor-pointer select-none hover:text-surface-900 dark:hover:text-white transition-colors font-semibold"
                    onClick={() => handleSort('logLoss')}
                  >
                    Log Loss{arrow('logLoss')}
                  </th>
                </tr>
              </thead>
              <tbody>
                {sorted.map((team) => (
                  <TeamRow key={team.id} team={team} season={season} />
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
