const BASE_URL = import.meta.env.VITE_API_URL || '';

export interface JobInfo {
  id: string;
  name: string;
  status: 'idle' | 'running' | 'completed' | 'failed';
  startedAt: string | null;
  finishedAt: string | null;
  error: string | null;
  output: string;
}

export interface JobStatuses {
  dataCollection: JobInfo;
  prediction: JobInfo;
}

export async function getJobStatuses(): Promise<JobStatuses> {
  const res = await fetch(`${BASE_URL}/api/Admin/GetJobStatuses`);
  if (!res.ok) throw new Error(`API error: ${res.status}`);
  return res.json();
}

export async function startDataCollection(): Promise<{ message: string }> {
  const res = await fetch(`${BASE_URL}/api/Admin/StartDataCollection`, { method: 'POST' });
  if (!res.ok) {
    const body = await res.json().catch(() => null);
    throw new Error(body?.message || `API error: ${res.status}`);
  }
  return res.json();
}

export async function startPrediction(): Promise<{ message: string }> {
  const res = await fetch(`${BASE_URL}/api/Admin/StartPrediction`, { method: 'POST' });
  if (!res.ok) {
    const body = await res.json().catch(() => null);
    throw new Error(body?.message || `API error: ${res.status}`);
  }
  return res.json();
}
