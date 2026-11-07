import { ApiError, apiFetch } from './client';
import type { ActivePlayerVM, GamePlayersVM, PlayerVM, RosterPlayerVM } from '../types';

async function orUndefinedOn404<T>(request: Promise<T>): Promise<T | undefined> {
  try {
    return await request;
  } catch (e) {
    if (e instanceof ApiError && e.status === 404) return undefined;
    throw e;
  }
}

// undefined when there's no such player
export function getPlayer(
  playerId: string,
  seasonStartYear: number,
  init?: RequestInit,
): Promise<PlayerVM | undefined> {
  if (!/^\d+$/.test(playerId)) return Promise.resolve(undefined);
  const params = new URLSearchParams({ playerId, seasonStartYear: String(seasonStartYear) });
  return orUndefinedOn404(apiFetch<PlayerVM>(`/api/Player/GetPlayer?${params}`, init));
}

export function getTeamRoster(
  teamId: number,
  seasonStartYear: number,
  init?: RequestInit,
): Promise<RosterPlayerVM[]> {
  const params = new URLSearchParams({
    teamId: String(teamId),
    seasonStartYear: String(seasonStartYear),
  });
  return apiFetch<RosterPlayerVM[]>(`/api/Player/GetTeamRoster?${params}`, init);
}

// undefined when there's no such game
export function getGamePlayers(
  gameId: string,
  init?: RequestInit,
): Promise<GamePlayersVM | undefined> {
  if (!/^\d+$/.test(gameId)) return Promise.resolve(undefined);
  return orUndefinedOn404(
    apiFetch<GamePlayersVM>(`/api/Player/GetGamePlayers?gameId=${gameId}`, init),
  );
}

export function getActivePlayers(seasonStartYear: number): Promise<ActivePlayerVM[]> {
  return apiFetch<ActivePlayerVM[]>(
    `/api/Player/GetActivePlayers?seasonStartYear=${seasonStartYear}`,
  );
}
