import { ApiError, apiFetch } from './client';
import type { GameOddsVM } from '../types';

export interface AnchorDateVM {
  anchorDate: string | null;
}

export function getGameOddsInDateRange(
  startDate: string,
  endDate: string,
  seasonStartYear: number,
  init?: RequestInit,
): Promise<GameOddsVM[]> {
  const params = new URLSearchParams({
    startDate,
    endDate,
    seasonStartYear: String(seasonStartYear),
  });
  return apiFetch<GameOddsVM[]>(`/api/GameOdds/GetGameOddsInDateRange?${params}`, init);
}

// undefined when there's no such game
export async function getGame(gameId: string, init?: RequestInit): Promise<GameOddsVM | undefined> {
  if (!/^\d+$/.test(gameId)) return undefined;
  try {
    return await apiFetch<GameOddsVM>(`/api/GameOdds/GetGame?gameId=${gameId}`, init);
  } catch (e) {
    if (e instanceof ApiError && e.status === 404) return undefined;
    throw e;
  }
}

export async function getAnchorDate(
  seasonStartYear: number,
  init?: RequestInit,
): Promise<AnchorDateVM> {
  const params = new URLSearchParams({
    seasonStartYear: String(seasonStartYear),
  });
  const raw = await apiFetch<{ anchorDate: string | null }>(
    `/api/GameOdds/GetAnchorDate?${params}`,
    init,
  );
  return {
    anchorDate: raw.anchorDate ? raw.anchorDate.slice(0, 10) : null,
  };
}
