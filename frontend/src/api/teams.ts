import { apiFetch } from './client';
import type { TeamsVM, TeamVM } from '../types';

export function getAllTeams(seasonStartYear: number): Promise<TeamsVM> {
  return apiFetch<TeamsVM>(`/api/Team/GetAllTeams?seasonStartYear=${seasonStartYear}`);
}

export function getTeam(teamId: number, seasonStartYear: number): Promise<TeamVM> {
  return apiFetch<TeamVM>(`/api/Team/GetTeam?teamId=${teamId}&seasonStartYear=${seasonStartYear}`);
}
