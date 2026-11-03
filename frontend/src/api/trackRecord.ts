import { apiFetch } from './client';
import type { TrackRecordVM } from '../types';

export function getTrackRecord(
  seasonStartYear: number,
  liveOnly: boolean,
  init?: RequestInit,
): Promise<TrackRecordVM> {
  const params = new URLSearchParams({
    seasonStartYear: String(seasonStartYear),
    liveOnly: String(liveOnly),
  });
  return apiFetch<TrackRecordVM>(`/api/TrackRecord/GetTrackRecord?${params}`, init);
}
