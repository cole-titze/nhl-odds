import { useState, useEffect, useCallback, useRef } from 'react';
import {
  getErrorLogs,
  getHealthChecks,
  getJobStatuses,
  startOddsBackfill,
  startPredictionBackfill,
  type ErrorLog,
  type JobInfo,
  type JobStatuses,
  type SeasonHealthCheck,
} from '../api/admin';
import { formatSeasonLabel } from '../utils/season';
import { JobCardSkeleton, HealthCheckTableSkeleton } from '../components/Skeleton';

const STATUS_STYLES: Record<string, string> = {
  idle: 'bg-surface-200 dark:bg-white/[0.06] text-surface-500 dark:text-surface-400',
  running: 'bg-amber-500/15 text-amber-600 dark:text-amber-400',
  completed: 'bg-emerald-500/15 text-emerald-600 dark:text-emerald-400',
  failed: 'bg-red-500/15 text-red-600 dark:text-red-400',
};

function formatTime(iso: string | null) {
  if (!iso) return '-';
  const d = new Date(iso);
  if (isNaN(d.getTime())) return '-';
  return d.toLocaleString();
}

function ElapsedTime({ startedAt }: { startedAt: string | null }) {
  const [elapsed, setElapsed] = useState('');

  useEffect(() => {
    if (!startedAt) return;
    const start = new Date(startedAt).getTime();
    if (isNaN(start)) return;

    function update() {
      const secs = Math.floor((Date.now() - start) / 1000);
      const m = Math.floor(secs / 60);
      const s = secs % 60;
      setElapsed(m > 0 ? `${m}m ${s}s` : `${s}s`);
    }

    update();
    const interval = setInterval(update, 1000);
    return () => clearInterval(interval);
  }, [startedAt]);

  return <span className="stat-number text-xs">{elapsed}</span>;
}

function isToday(iso: string | null): boolean {
  if (!iso) return false;
  const d = new Date(iso);
  const now = new Date();
  return d.toDateString() === now.toDateString();
}

function JobCard({ job, label, onStart }: { job: JobInfo; label: string; onStart?: () => void }) {
  const isRunning = job.status === 'running';
  const ranToday = job.status === 'completed' && isToday(job.finishedAt);
  const detailsRef = useRef<HTMLDetailsElement>(null);

  return (
    <div className={`glass rounded-xl p-6 relative overflow-hidden`}>
      {isRunning && (
        <div className="absolute bottom-0 left-0 right-0 h-1 bg-surface-200 dark:bg-white/[0.06]">
          <div className="h-full bg-amber-500 dark:bg-amber-400 animate-progress-bar" />
        </div>
      )}

      <div className="flex items-center justify-between mb-4">
        <h2 className="font-display text-lg font-semibold">{label}</h2>
        <span
          className={`px-2.5 py-1 rounded-lg text-xs font-mono font-semibold uppercase ${STATUS_STYLES[job.status] || STATUS_STYLES.idle}`}
        >
          {job.status}
        </span>
      </div>

      <div className="space-y-2 text-sm mb-5">
        {isRunning ? (
          <div className="flex justify-between">
            <span className="text-surface-500 dark:text-surface-400">Elapsed</span>
            <ElapsedTime startedAt={job.startedAt} />
          </div>
        ) : (
          <>
            <div className="flex justify-between">
              <span className="text-surface-500 dark:text-surface-400">Started</span>
              <span className="stat-number text-xs">{formatTime(job.startedAt)}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-surface-500 dark:text-surface-400">Finished</span>
              <span className="stat-number text-xs">{formatTime(job.finishedAt)}</span>
            </div>
          </>
        )}
        {job.error && (
          <div className="mt-2 p-3 rounded-lg bg-red-500/10 text-red-600 dark:text-red-400 text-xs font-mono break-all">
            {job.error}
          </div>
        )}
      </div>

      {job.output && (
        <details ref={detailsRef} className="mb-5">
          <summary className="cursor-pointer text-xs font-medium text-surface-500 dark:text-surface-400 hover:text-surface-700 dark:hover:text-surface-300 select-none">
            Output
          </summary>
          <div className="mt-2 max-h-64 overflow-y-auto rounded-lg bg-surface-900 dark:bg-black/40 p-3">
            <pre className="text-xs font-mono text-surface-300 dark:text-surface-400 whitespace-pre-wrap break-all">
              {job.output}
            </pre>
          </div>
        </details>
      )}

      {onStart && (
        <button
          onClick={onStart}
          disabled={isRunning || ranToday}
          className={`w-full py-2.5 rounded-lg text-sm font-semibold transition-all ${
            isRunning || ranToday
              ? 'bg-surface-200 dark:bg-white/[0.04] text-surface-400 dark:text-surface-500 cursor-not-allowed'
              : 'bg-accent-500 hover:bg-accent-600 text-white shadow-lg shadow-accent-500/25'
          }`}
        >
          {isRunning ? 'Running...' : ranToday ? 'Completed Today' : `Start ${label}`}
        </button>
      )}
    </div>
  );
}

function countClass(count: number) {
  if (count < 0) return 'text-surface-400 dark:text-surface-500';
  if (count === 0) return 'text-emerald-500';
  return 'text-red-500 dark:text-red-400';
}

function countDisplay(count: number) {
  return count < 0 ? '-' : count;
}

function HealthCheckRow({ check }: { check: SeasonHealthCheck }) {
  const [expanded, setExpanded] = useState(false);
  const [errors, setErrors] = useState<ErrorLog[]>([]);
  const [loadingErrors, setLoadingErrors] = useState(false);

  async function handleToggle() {
    const next = !expanded;
    setExpanded(next);
    if (next && errors.length === 0) {
      setLoadingErrors(true);
      try {
        const data = await getErrorLogs(check.seasonStartYear);
        setErrors(data);
      } catch {
        /* ignore */
      } finally {
        setLoadingErrors(false);
      }
    }
  }

  const hasIssues =
    check.missingPredictions > 0 ||
    check.missingBookmakerOdds > 0 ||
    check.missingGameCleaned > 0 ||
    check.missingOddsFetchDays > 0 ||
    check.liveBookmakerOdds > 0 ||
    check.errorCount > 0;

  const cd = countDisplay;

  return (
    <>
      <tr
        onClick={handleToggle}
        className={`border-b border-surface-200 dark:border-white/[0.04] last:border-0 cursor-pointer transition-colors hover:bg-surface-100/50 dark:hover:bg-white/[0.03] ${expanded ? 'bg-surface-100/30 dark:bg-white/[0.02]' : ''}`}
      >
        <td className="px-4 py-3 text-sm font-mono font-medium">
          <span className="mr-1.5 text-surface-400 dark:text-surface-500 text-xs">
            {expanded ? '\u25BC' : '\u25B6'}
          </span>
          {formatSeasonLabel(check.seasonStartYear)}
        </td>
        <td className="px-4 py-3 stat-number text-sm text-center">{check.totalGames}</td>
        <td className="px-4 py-3 stat-number text-sm text-center">{check.playedGames}</td>
        <td
          className={`px-4 py-3 stat-number text-sm text-center ${countClass(check.missingPredictions)}`}
        >
          {cd(check.missingPredictions)}
        </td>
        <td
          className={`px-4 py-3 stat-number text-sm text-center ${countClass(check.missingBookmakerOdds)}`}
        >
          {cd(check.missingBookmakerOdds)}
        </td>
        <td
          className={`px-4 py-3 stat-number text-sm text-center ${countClass(check.missingGameCleaned)}`}
        >
          {check.missingGameCleaned}
        </td>
        <td
          className={`px-4 py-3 stat-number text-sm text-center ${countClass(check.missingOddsFetchDays)}`}
        >
          {cd(check.missingOddsFetchDays)}
        </td>
        <td
          className={`px-4 py-3 stat-number text-sm text-center ${countClass(check.liveBookmakerOdds)}`}
        >
          {check.liveBookmakerOdds}
        </td>
        <td className={`px-4 py-3 stat-number text-sm text-center ${countClass(check.errorCount)}`}>
          {check.errorCount}
        </td>
        <td className="px-4 py-3 text-center">
          {hasIssues ? (
            <span className="inline-block w-2 h-2 rounded-full bg-amber-500" />
          ) : (
            <span className="inline-block w-2 h-2 rounded-full bg-emerald-500" />
          )}
        </td>
      </tr>
      {expanded && (
        <tr>
          <td colSpan={10} className="p-0">
            <div className="px-4 py-3 bg-surface-50/50 dark:bg-white/[0.01]">
              {loadingErrors && (
                <div className="text-xs text-surface-400 dark:text-surface-500 py-2">
                  Loading errors...
                </div>
              )}
              {!loadingErrors && errors.length === 0 && (
                <div className="text-xs text-surface-400 dark:text-surface-500 py-2">
                  No errors for this season.
                </div>
              )}
              {!loadingErrors && errors.length > 0 && (
                <table className="w-full text-sm">
                  <thead>
                    <tr className="text-left text-xs text-surface-500 dark:text-surface-400 uppercase tracking-wide">
                      <th className="px-3 py-2">Time</th>
                      <th className="px-3 py-2">Source</th>
                      <th className="px-3 py-2">Exception</th>
                      <th className="px-3 py-2">Message</th>
                    </tr>
                  </thead>
                  <tbody>
                    {errors.map((log) => (
                      <tr
                        key={log.id}
                        className="border-t border-surface-200/50 dark:border-white/[0.03]"
                      >
                        <td className="px-3 py-2 stat-number text-xs whitespace-nowrap text-surface-500 dark:text-surface-400">
                          {formatTime(log.timestampUTC)}
                        </td>
                        <td className="px-3 py-2 text-xs font-mono text-surface-600 dark:text-surface-300 whitespace-nowrap">
                          {log.source}
                        </td>
                        <td className="px-3 py-2 text-xs font-mono text-red-600 dark:text-red-400 whitespace-nowrap">
                          {log.exceptionType}
                        </td>
                        <td className="px-3 py-2 text-xs text-surface-700 dark:text-surface-300">
                          <details>
                            <summary className="cursor-pointer truncate max-w-md select-none">
                              {log.message}
                            </summary>
                            <pre className="mt-2 p-2 rounded bg-surface-900 dark:bg-black/40 text-surface-300 dark:text-surface-400 whitespace-pre-wrap break-all text-xs font-mono">
                              {log.stackTrace}
                            </pre>
                          </details>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </div>
          </td>
        </tr>
      )}
    </>
  );
}

export function AdminPage() {
  const [statuses, setStatuses] = useState<JobStatuses | null>(null);
  const [healthChecks, setHealthChecks] = useState<SeasonHealthCheck[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  const refresh = useCallback(async () => {
    try {
      const [statusData, checksData] = await Promise.all([getJobStatuses(), getHealthChecks()]);
      setStatuses(statusData);
      setHealthChecks(checksData);
      setError(null);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to fetch statuses');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect
    refresh();
    const interval = setInterval(refresh, 3000);
    return () => clearInterval(interval);
  }, [refresh]);

  async function handleStart(action: () => Promise<{ message: string }>) {
    try {
      await action();
      await refresh();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to start job');
    }
  }

  return (
    <div>
      <div className="mb-8">
        <h1 className="font-display text-3xl font-bold tracking-tight">Admin</h1>
        <p className="text-sm text-surface-500 dark:text-surface-400 mt-1">
          Manage data collection and prediction jobs
        </p>
      </div>

      {error && (
        <div className="glass rounded-xl text-center text-red-500 py-4 mb-6 text-sm">{error}</div>
      )}

      {loading ? (
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-6">
          <JobCardSkeleton />
          <JobCardSkeleton />
          <JobCardSkeleton />
          <JobCardSkeleton />
          <JobCardSkeleton />
          <JobCardSkeleton />
        </div>
      ) : statuses ? (
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-6">
          <JobCard
            job={statuses.oddsBackfill}
            label="Odds Backfill"
            onStart={() => handleStart(startOddsBackfill)}
          />
          <JobCard
            job={statuses.predictionBackfill}
            label="Prediction Backfill"
            onStart={() => handleStart(startPredictionBackfill)}
          />
          <JobCard job={statuses.dataCollection} label="Data Collection" />
          <JobCard job={statuses.oddsFetch} label="Odds Fetch" />
          <JobCard job={statuses.kalshiFetch} label="Kalshi Fetch" />
          <JobCard job={statuses.prediction} label="Prediction" />
        </div>
      ) : null}

      <div className="mt-10">
        <h2 className="font-display text-xl font-semibold mb-4">Health Checks</h2>
        {loading ? (
          <HealthCheckTableSkeleton />
        ) : healthChecks.length === 0 ? (
          <div className="glass rounded-xl p-6 text-sm text-surface-500 dark:text-surface-400 text-center">
            No data available.
          </div>
        ) : (
          <div className="glass rounded-xl overflow-hidden">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-surface-200 dark:border-white/[0.06] text-left text-xs text-surface-500 dark:text-surface-400 uppercase tracking-wide">
                  <th className="px-4 py-3">Season</th>
                  <th className="px-4 py-3 text-center">Total</th>
                  <th className="px-4 py-3 text-center">Played</th>
                  <th className="px-4 py-3 text-center">No In-House Odds</th>
                  <th className="px-4 py-3 text-center">No Bookmaker Odds</th>
                  <th className="px-4 py-3 text-center">No Cleaned Data</th>
                  <th className="px-4 py-3 text-center">No Odds Fetch</th>
                  <th className="px-4 py-3 text-center">Live Odds</th>
                  <th className="px-4 py-3 text-center">Errors</th>
                  <th className="px-4 py-3 text-center w-10"></th>
                </tr>
              </thead>
              <tbody>
                {healthChecks.map((check) => (
                  <HealthCheckRow key={check.seasonStartYear} check={check} />
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}
