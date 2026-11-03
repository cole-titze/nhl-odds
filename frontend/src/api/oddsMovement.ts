import { apiFetch } from './client';
import type { OddsMovementVM } from '../types';

export function getOddsMovement(gameId: number, init?: RequestInit): Promise<OddsMovementVM> {
  return apiFetch<OddsMovementVM>(`/api/OddsMovement/GetOddsMovement?gameId=${gameId}`, init);
}
