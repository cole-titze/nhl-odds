import { useState, useEffect, useCallback, useRef } from 'react';
import {
  getErrorLogs,
  getJobStatuses,
  startDataCollection,
  startPrediction,
  type ErrorLog,
  type JobInfo,
  type JobStatuses,
} from '../api/admin';

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

function JobCard({ job, label, onStart }: { job: JobInfo; label: string; onStart: () => void }) {
  const isRunning = job.status === 'running';
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

      <button
        onClick={onStart}
        disabled={isRunning}
        className={`w-full py-2.5 rounded-lg text-sm font-semibold transition-all ${
          isRunning
            ? 'bg-surface-200 dark:bg-white/[0.04] text-surface-400 dark:text-surface-500 cursor-not-allowed'
            : 'bg-accent-500 hover:bg-accent-600 text-white shadow-lg shadow-accent-500/25'
        }`}
      >
        {isRunning ? 'Running...' : `Start ${label}`}
      </button>
    </div>
  );
}

export function AdminPage() {
  const [statuses, setStatuses] = useState<JobStatuses | null>(null);
  const [errorLogs, setErrorLogs] = useState<ErrorLog[]>([]);
  const [error, setError] = useState<string | null>(null);

  const refresh = useCallback(async () => {
    try {
      const [statusData, logsData] = await Promise.all([getJobStatuses(), getErrorLogs()]);
      setStatuses(statusData);
      setErrorLogs(logsData);
      setError(null);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to fetch statuses');
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

      {statuses && (
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-6">
          <JobCard
            job={statuses.dataCollection}
            label="Data Collection"
            onStart={() => handleStart(startDataCollection)}
          />
          <JobCard
            job={statuses.prediction}
            label="Prediction"
            onStart={() => handleStart(startPrediction)}
          />
        </div>
      )}

      <div className="mt-10">
        <h2 className="font-display text-xl font-semibold mb-4">Error Log</h2>
        {errorLogs.length === 0 ? (
          <div className="glass rounded-xl p-6 text-sm text-surface-500 dark:text-surface-400 text-center">
            No errors logged.
          </div>
        ) : (
          <div className="glass rounded-xl overflow-hidden">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-surface-200 dark:border-white/[0.06] text-left text-xs text-surface-500 dark:text-surface-400 uppercase tracking-wide">
                  <th className="px-4 py-3">Time</th>
                  <th className="px-4 py-3">Source</th>
                  <th className="px-4 py-3">Exception</th>
                  <th className="px-4 py-3">Message</th>
                </tr>
              </thead>
              <tbody>
                {errorLogs.map((log) => (
                  <tr
                    key={log.id}
                    className="border-b border-surface-200 dark:border-white/[0.04] last:border-0"
                  >
                    <td className="px-4 py-3 stat-number text-xs whitespace-nowrap text-surface-500 dark:text-surface-400">
                      {formatTime(log.timestampUTC)}
                    </td>
                    <td className="px-4 py-3 text-xs font-mono text-surface-600 dark:text-surface-300 whitespace-nowrap">
                      {log.source}
                    </td>
                    <td className="px-4 py-3 text-xs font-mono text-red-600 dark:text-red-400 whitespace-nowrap">
                      {log.exceptionType}
                    </td>
                    <td className="px-4 py-3 text-xs text-surface-700 dark:text-surface-300">
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
          </div>
        )}
      </div>
    </div>
  );
}
