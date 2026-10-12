import { apiFetch } from './client';
import type { TeamsVM, TeamVM } from '../types';

export function getAllTeams(seasonStartYear: number, init?: RequestInit): Promise<TeamsVM> {
  return apiFetch<TeamsVM>(`/api/Team/GetAllTeams?seasonStartYear=${seasonStartYear}`, init);
}

export function getTeam(
  teamId: number,
  seasonStartYear: number,
  init?: RequestInit,
): Promise<TeamVM> {
  return apiFetch<TeamVM>(
    `/api/Team/GetTeam?teamId=${teamId}&seasonStartYear=${seasonStartYear}`,
    init,
  );
}
