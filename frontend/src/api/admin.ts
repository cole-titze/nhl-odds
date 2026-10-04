import { clearAdminKey, getAdminKey } from '../hooks/useAdminKey';

const BASE_URL = import.meta.env.VITE_API_URL || '';

/** Thrown when the API rejects the admin key (401) or has none configured (403). */
export class AdminAuthError extends Error {}

async function adminFetch(path: string, init?: RequestInit): Promise<Response> {
  const key = getAdminKey();
  const res = await fetch(`${BASE_URL}/api/Admin/${path}`, {
    ...init,
    headers: key ? { 'X-Admin-Key': key } : undefined,
  });
  if (res.status === 401) {
    clearAdminKey();
    throw new AdminAuthError('That admin key was rejected.');
  }
  if (res.status === 403) {
    throw new AdminAuthError('The API has no admin key configured.');
  }
  return res;
}

export interface ErrorLog {
  id: number;
  timestampUTC: string;
  gameId: number | null;
  seasonStartYear: number | null;
  exceptionType: string;
  message: string;
  stackTrace: string;
  source: string;
}

export interface JobInfo {
  id: string;
  name: string;
  status: 'idle' | 'running' | 'completed' | 'failed' | 'requested';
  startedAt: string | null;
  finishedAt: string | null;
  error: string | null;
  output: string;
  completedToday: boolean;
}

export interface JobStatuses {
  dataCollection: JobInfo;
  oddsFetch: JobInfo;
  prediction: JobInfo;
  oddsBackfill: JobInfo;
  predictionBackfill: JobInfo;
  kalshiFetch: JobInfo;
  kalshiBackfill: JobInfo;
}

export async function getJobStatuses(): Promise<JobStatuses> {
  const res = await adminFetch('GetJobStatuses');
  if (!res.ok) throw new Error(`API error: ${res.status}`);
  return res.json();
}

export async function startDataCollection(): Promise<{ message: string }> {
  const res = await adminFetch('StartDataCollection', { method: 'POST' });
  if (!res.ok) {
    const body = await res.json().catch(() => null);
    throw new Error(body?.message || `API error: ${res.status}`);
  }
  return res.json();
}

export async function startPrediction(): Promise<{ message: string }> {
  const res = await adminFetch('StartPrediction', { method: 'POST' });
  if (!res.ok) {
    const body = await res.json().catch(() => null);
    throw new Error(body?.message || `API error: ${res.status}`);
  }
  return res.json();
}

export async function startOddsBackfill(): Promise<{ message: string }> {
  const res = await adminFetch('StartOddsBackfill', { method: 'POST' });
  if (!res.ok) {
    const body = await res.json().catch(() => null);
    throw new Error(body?.message || `API error: ${res.status}`);
  }
  return res.json();
}

export async function startPredictionBackfill(): Promise<{ message: string }> {
  const res = await adminFetch('StartPredictionBackfill', { method: 'POST' });
  if (!res.ok) {
    const body = await res.json().catch(() => null);
    throw new Error(body?.message || `API error: ${res.status}`);
  }
  return res.json();
}

export async function startKalshiBackfill(): Promise<{ message: string }> {
  const res = await adminFetch('StartKalshiBackfill', { method: 'POST' });
  if (!res.ok) {
    const body = await res.json().catch(() => null);
    throw new Error(body?.message || `API error: ${res.status}`);
  }
  return res.json();
}

export interface SeasonHealthCheck {
  seasonStartYear: number;
  totalGames: number;
  playedGames: number;
  missingPredictions: number;
  missingBookmakerOdds: number;
  missingGameCleaned: number;
  missingOddsFetchDays: number;
  liveBookmakerOdds: number;
  missingKalshiOdds: number;
  errorCount: number;
}

export async function getErrorLogs(seasonStartYear?: number): Promise<ErrorLog[]> {
  const params = seasonStartYear != null ? `?seasonStartYear=${seasonStartYear}` : '';
  const res = await adminFetch(`GetErrorLogs${params}`);
  if (!res.ok) throw new Error(`API error: ${res.status}`);
  return res.json();
}

export async function getHealthChecks(): Promise<SeasonHealthCheck[]> {
  const res = await adminFetch('GetHealthChecks');
  if (!res.ok) throw new Error(`API error: ${res.status}`);
  return res.json();
}
