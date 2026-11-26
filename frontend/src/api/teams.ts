import { ApiError, apiFetch } from './client';
import { getCurrentSeason } from '../utils/season';
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

/** null for a 404 (no such team that season) */
async function findTeam(teamId: number, season: number, init?: RequestInit) {
  try {
    return await getTeam(teamId, season, init);
  } catch (e) {
    if (e instanceof ApiError && e.status === 404) return null;
    throw e;
  }
}

/** The latest season the team played (a former team's last one), or null for an unknown team */
async function getTeamLastSeason(teamId: number, init?: RequestInit) {
  try {
    return await apiFetch<number>(`/api/Team/GetTeamLastSeason?teamId=${teamId}`, init);
  } catch (e) {
    if (e instanceof ApiError && e.status === 404) return null;
    throw e;
  }
}

export interface TeamPageData {
  team: TeamVM | null;
  season: number;
}

/**
 * A team's page: the season asked for, or with none, this season, or for a former team
 * (Coyotes, Utah Hockey Club) its last one, so /team/<id> is a real page for every team.
 */
export async function getTeamPage(
  teamId: number,
  season: number | null,
  init?: RequestInit,
): Promise<TeamPageData> {
  const asked = season ?? getCurrentSeason();
  const team = await findTeam(teamId, asked, init);
  if (team || season != null) return { team, season: asked };
  const last = await getTeamLastSeason(teamId, init);
  if (last == null || last === asked) return { team: null, season: asked };
  return { team: await findTeam(teamId, last, init), season: last };
}
