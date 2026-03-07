import { apiFetch } from './client';
import type { GameOddsVM } from '../types';

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
