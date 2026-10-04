import { useState, useEffect, useCallback, useRef } from 'react';
import {
  getErrorLogs,
  getHealthChecks,
  getJobStatuses,
  startDataCollection,
  startOddsBackfill,
  startPrediction,
  startPredictionBackfill,
  startKalshiBackfill,
  AdminAuthError,
  type ErrorLog,
  type JobInfo,
  type JobStatuses,
  type SeasonHealthCheck,
} from '../api/admin';
import { formatSeasonLabel } from '../utils/season';
import { JobCardSkeleton, HealthCheckTableSkeleton } from '../components/Skeleton';
import { LoadingStatus, PageTitle, ScrollRegion } from '../components/A11y';
import { clearAdminKey, setAdminKey, useAdminKey } from '../hooks/useAdminKey';

const STATUS_STYLES: Record<string, string> = {
  idle: 'bg-surface-200 dark:bg-white/[0.06] text-surface-600 dark:text-surface-400',
  running: 'bg-amber-500/15 text-amber-800 dark:text-amber-400',
  completed: 'bg-emerald-500/15 text-emerald-800 dark:text-emerald-400',
  failed: 'bg-red-500/15 text-red-700 dark:text-red-400',
  requested: 'bg-blue-500/15 text-blue-700 dark:text-blue-400',
};

function formatTime(iso: string | null) {
  if (!iso) return '-';
  // Server stores UTC — ensure the string is parsed as UTC if no timezone indicator
  const normalized = iso.endsWith('Z') || iso.includes('+') ? iso : iso + 'Z';
  const d = new Date(normalized);
  if (isNaN(d.getTime())) return '-';
  return d.toLocaleString();
}

function ElapsedTime({ startedAt }: { startedAt: string | null }) {
  const [elapsed, setElapsed] = useState('');

  useEffect(() => {
    if (!startedAt) return;
    const normalized =
      startedAt.endsWith('Z') || startedAt.includes('+') ? startedAt : startedAt + 'Z';
    const start = new Date(normalized).getTime();
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

function JobCard({ job, label, onStart }: { job: JobInfo; label: string; onStart?: () => void }) {
  const isRunning = job.status === 'running';
  const isRequested = job.status === 'requested';
  const ranToday = job.completedToday;
  const detailsRef = useRef<HTMLDetailsElement>(null);

  return (
    <div className={`glass rounded-xl p-6 relative overflow-hidden`}>
      {isRunning && (
        <div
          aria-hidden="true"
          className="absolute bottom-0 left-0 right-0 h-1 bg-surface-200 dark:bg-white/[0.06]"
        >
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
          <div className="mt-2 p-3 rounded-lg bg-red-500/10 text-red-700 dark:text-red-400 text-xs font-mono break-all">
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
          type="button"
          onClick={onStart}
          disabled={isRunning || isRequested || ranToday}
          className={`w-full py-2.5 rounded-lg text-sm font-semibold transition-all ${
            isRunning || isRequested || ranToday
              ? 'bg-surface-200 dark:bg-white/[0.04] text-surface-500 dark:text-surface-400 cursor-not-allowed'
              : 'bg-accent-600 hover:bg-accent-700 text-on-accent shadow-lg shadow-accent-500/25'
          }`}
        >
          {isRunning
            ? 'Running...'
            : isRequested
              ? 'Requested...'
              : ranToday
                ? 'Completed Today'
                : `Start ${label}`}
        </button>
      )}
    </div>
  );
}

function countClass(count: number) {
  if (count < 0) return 'text-surface-500 dark:text-surface-400';
  if (count === 0) return 'text-emerald-700 dark:text-emerald-400';
  return 'text-red-700 dark:text-red-400';
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
          {/* Click bubbles to the row's toggle handler */}
          <button
            type="button"
            aria-expanded={expanded}
            className="font-mono font-medium cursor-pointer whitespace-nowrap"
          >
            <span
              aria-hidden="true"
              className="mr-1.5 text-surface-500 dark:text-surface-400 text-xs"
            >
              {expanded ? '\u25BC' : '\u25B6'}
            </span>
            {formatSeasonLabel(check.seasonStartYear)}
            <span className="sr-only"> error log</span>
          </button>
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
        <td
          className={`px-4 py-3 stat-number text-sm text-center ${countClass(check.missingKalshiOdds)}`}
        >
          {cd(check.missingKalshiOdds)}
        </td>
        <td className={`px-4 py-3 stat-number text-sm text-center ${countClass(check.errorCount)}`}>
          {check.errorCount}
        </td>
        <td className="px-4 py-3 text-center">
          {hasIssues ? (
            <span title="Has issues" className="inline-block w-2 h-2 rounded-full bg-amber-500">
              <span className="sr-only">Has issues</span>
            </span>
          ) : (
            <span title="OK" className="inline-block w-2 h-2 rounded-full bg-emerald-500">
              <span className="sr-only">OK</span>
            </span>
          )}
        </td>
      </tr>
      {expanded && (
        <tr>
          <td colSpan={10} className="p-0">
            <div className="px-4 py-3 bg-surface-50/50 dark:bg-white/[0.01]">
              {loadingErrors && (
                <div role="status" className="text-xs text-surface-500 dark:text-surface-400 py-2">
                  Loading errors...
                </div>
              )}
              {!loadingErrors && errors.length === 0 && (
                <div className="text-xs text-surface-500 dark:text-surface-400 py-2">
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
                        <td className="px-3 py-2 text-xs font-mono text-red-700 dark:text-red-400 whitespace-nowrap">
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

function AdminKeyForm({
  message,
  onSubmit,
}: {
  message: string | null;
  onSubmit: (key: string) => void;
}) {
  const [value, setValue] = useState('');

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        if (value.trim()) onSubmit(value.trim());
      }}
      className="glass rounded-xl p-6 max-w-md"
    >
      <label htmlFor="admin-key" className="block text-sm font-medium mb-2">
        Admin key
      </label>
      <input
        id="admin-key"
        type="password"
        autoComplete="current-password"
        value={value}
        onChange={(e) => setValue(e.target.value)}
        aria-describedby={message ? 'admin-key-error' : undefined}
        className="w-full px-3 py-2 rounded-lg glass text-sm font-mono"
      />
      {message && (
        <p
          id="admin-key-error"
          role="alert"
          className="mt-2 text-sm text-red-700 dark:text-red-400"
        >
          {message}
        </p>
      )}
      <button
        type="submit"
        className="mt-4 w-full py-2.5 rounded-lg text-sm font-semibold transition-all bg-accent-600 hover:bg-accent-700 text-on-accent shadow-lg shadow-accent-500/25"
      >
        Unlock
      </button>
    </form>
  );
}

export function AdminPage() {
  const adminKey = useAdminKey();
  const [authError, setAuthError] = useState<string | null>(null);
  // Dev APIs usually run without a key, so only ask for one there once the API rejects a request
  const needsKey = adminKey == null && (!import.meta.env.DEV || authError != null);
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
      if (e instanceof AdminAuthError) setAuthError(e.message);
      setError(e instanceof Error ? e.message : 'Failed to fetch statuses');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (needsKey) return;
    refresh();
    const interval = setInterval(refresh, 3000);
    return () => clearInterval(interval);
  }, [refresh, needsKey]);

  function handleUnlock(key: string) {
    setAuthError(null);
    setError(null);
    setAdminKey(key);
  }

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
      <PageTitle title="Admin" />
      <div className="mb-8">
        <h1 className="font-display text-3xl font-bold tracking-tight">Admin</h1>
        <p className="text-sm text-surface-500 dark:text-surface-400 mt-1">
          Manage data collection and prediction jobs
        </p>
        {adminKey != null && (
          <button
            type="button"
            onClick={clearAdminKey}
            className="mt-2 text-sm underline text-surface-600 dark:text-surface-300 hover:text-surface-900 dark:hover:text-white"
          >
            Forget admin key
          </button>
        )}
      </div>

      {needsKey ? (
        <AdminKeyForm message={authError} onSubmit={handleUnlock} />
      ) : (
        <>
          {error && (
            <div
              role="alert"
              className="glass rounded-xl text-center text-red-700 dark:text-red-400 py-4 mb-6 text-sm"
            >
              {error}
            </div>
          )}

          {loading ? (
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-6">
              <LoadingStatus label="Loading job statuses…" />
              <JobCardSkeleton />
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
                job={statuses.dataCollection}
                label="Data Collection"
                onStart={() => handleStart(startDataCollection)}
              />
              <JobCard
                job={statuses.predictionBackfill}
                label="Prediction Backfill"
                onStart={() => handleStart(startPredictionBackfill)}
              />
              <JobCard
                job={statuses.oddsBackfill}
                label="Odds Backfill"
                onStart={() => handleStart(startOddsBackfill)}
              />
              <JobCard
                job={statuses.kalshiBackfill}
                label="Kalshi Backfill"
                onStart={() => handleStart(startKalshiBackfill)}
              />
              <JobCard job={statuses.kalshiFetch} label="Kalshi Fetch" />
              <JobCard job={statuses.oddsFetch} label="Odds Fetch" />
              <JobCard
                job={statuses.prediction}
                label="Prediction"
                onStart={() => handleStart(startPrediction)}
              />
            </div>
          ) : null}

          <div className="mt-10">
            <h2 className="font-display text-xl font-semibold mb-4">Health Checks</h2>

            <details className="mb-4 glass rounded-xl">
              <summary className="px-4 py-3 text-sm font-medium cursor-pointer select-none text-surface-600 dark:text-surface-300 hover:text-surface-900 dark:hover:text-white">
                Column descriptions
              </summary>
              <div className="px-4 pb-4 pt-2 grid grid-cols-1 sm:grid-cols-2 gap-x-8 gap-y-2 text-sm text-surface-600 dark:text-surface-400">
                <div>
                  <span className="font-medium text-surface-800 dark:text-surface-200">Season</span>{' '}
                  — NHL season start year (e.g. 2024 = 2024–25 season).
                </div>
                <div>
                  <span className="font-medium text-surface-800 dark:text-surface-200">Total</span>{' '}
                  — Total regular-season games scheduled that season.
                </div>
                <div>
                  <span className="font-medium text-surface-800 dark:text-surface-200">Played</span>{' '}
                  — Games confirmed completed in the database.
                </div>
                <div>
                  <span className="font-medium text-surface-800 dark:text-surface-200">
                    No In-House Odds
                  </span>{' '}
                  — Played games missing a model-generated win probability.
                </div>
                <div>
                  <span className="font-medium text-surface-800 dark:text-surface-200">
                    No Bookmaker Odds
                  </span>{' '}
                  — Played games with no historical bookmaker lines stored.
                </div>
                <div>
                  <span className="font-medium text-surface-800 dark:text-surface-200">
                    No Cleaned Data
                  </span>{' '}
                  — Games missing the cleaned/featurized row used for ML training.
                </div>
                <div>
                  <span className="font-medium text-surface-800 dark:text-surface-200">
                    No Odds Fetch
                  </span>{' '}
                  — Game days where no pre-game odds fetch was recorded.
                </div>
                <div>
                  <span className="font-medium text-surface-800 dark:text-surface-200">
                    Live Odds
                  </span>{' '}
                  — Games where the stored bookmaker odds were last updated after the game started.
                  These are in-game or post-game lines, not pre-game, so they're unreliable as
                  prediction inputs. Should be 0.
                </div>
                <div>
                  <span className="font-medium text-surface-800 dark:text-surface-200">
                    No Kalshi
                  </span>{' '}
                  — Played games with no Kalshi market odds stored.
                </div>
                <div>
                  <span className="font-medium text-surface-800 dark:text-surface-200">Errors</span>{' '}
                  — Pipeline errors logged for that season. Select a season to expand its error log.
                </div>
              </div>
            </details>

            {loading ? (
              <HealthCheckTableSkeleton />
            ) : healthChecks.length === 0 ? (
              <div className="glass rounded-xl p-6 text-sm text-surface-500 dark:text-surface-400 text-center">
                No data available.
              </div>
            ) : (
              <ScrollRegion label="Health checks" className="glass rounded-xl overflow-x-auto">
                <table className="w-full text-sm min-w-[700px]">
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
                      <th className="px-4 py-3 text-center">No Kalshi</th>
                      <th className="px-4 py-3 text-center">Errors</th>
                      <th className="px-4 py-3 text-center w-10">
                        <span className="sr-only">Status</span>
                      </th>
                    </tr>
                  </thead>
                  <tbody>
                    {healthChecks.map((check) => (
                      <HealthCheckRow key={check.seasonStartYear} check={check} />
                    ))}
                  </tbody>
                </table>
              </ScrollRegion>
            )}
          </div>
        </>
      )}
    </div>
  );
}
