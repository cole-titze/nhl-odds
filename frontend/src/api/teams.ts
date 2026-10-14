import { apiFetch } from './client';
import type { SeasonBookmakerOddsVM, SeasonTeamsVM, TeamsVM, TeamVM } from '../types';

export function getAllTeams(seasonStartYear: number, init?: RequestInit): Promise<TeamsVM> {
  return apiFetch<TeamsVM>(`/api/Team/GetAllTeams?seasonStartYear=${seasonStartYear}`, init);
}

export function getSeasonTeams(
  seasonStartYear: number,
  init?: RequestInit,
): Promise<SeasonTeamsVM> {
  return apiFetch<SeasonTeamsVM>(
    `/api/Team/GetSeasonTeams?seasonStartYear=${seasonStartYear}`,
    init,
  );
}

export function getSeasonBookmakerOdds(
  seasonStartYear: number,
  bookmaker: string,
): Promise<SeasonBookmakerOddsVM[]> {
  const params = new URLSearchParams({ seasonStartYear: String(seasonStartYear), bookmaker });
  return apiFetch<SeasonBookmakerOddsVM[]>(`/api/Team/GetSeasonBookmakerOdds?${params}`);
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
