import { apiFetch } from './client';
import type { GameLineupResultVM } from '../types';

export function getGameLineup(gameId: number, init?: RequestInit): Promise<GameLineupResultVM> {
  return apiFetch<GameLineupResultVM>(`/api/Lineup/GetGameLineup?gameId=${gameId}`, init);
}
