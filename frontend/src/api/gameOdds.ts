import { apiFetch } from './client';
import type { GameOddsVM } from '../types';

export interface AnchorDateVM {
  anchorDate: string | null;
}

export function getGameOddsInDateRange(
  startDate: string,
  endDate: string,
  seasonStartYear: number,
): Promise<GameOddsVM[]> {
  const params = new URLSearchParams({
    startDate,
    endDate,
    seasonStartYear: String(seasonStartYear),
  });
  return apiFetch<GameOddsVM[]>(`/api/GameOdds/GetGameOddsInDateRange?${params}`);
}

export async function getAnchorDate(seasonStartYear: number): Promise<AnchorDateVM> {
  const params = new URLSearchParams({
    seasonStartYear: String(seasonStartYear),
  });
  const raw = await apiFetch<{ anchorDate: string | null }>(
    `/api/GameOdds/GetAnchorDate?${params}`,
  );
  return {
    anchorDate: raw.anchorDate ? raw.anchorDate.slice(0, 10) : null,
  };
}
