import { apiFetch } from './client';
import type { StatLeadersVM } from '../types';

/** The season's top 50 in one stat (see LEADER_STATS) */
export function getStatLeaders(
  stat: string,
  seasonStartYear: number,
  init?: RequestInit,
): Promise<StatLeadersVM> {
  const params = new URLSearchParams({ stat, seasonStartYear: String(seasonStartYear) });
  return apiFetch<StatLeadersVM>(`/api/Leaders/GetStatLeaders?${params}`, init);
}
