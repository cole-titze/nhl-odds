import { apiFetch } from './client';
import type { BetTypeCategory, StrategyType } from '../utils/bettingStrategies';

export interface BestStrategyVM {
  betType: BetTypeCategory;
  strategyType: StrategyType;
  threshold: number;
  bets: number;
  wins: number;
  roi: number;
  firstSeason: number;
  lastSeason: number;
}

// Best historical strategy per bet type, backtested and cached by the API.
// Bet types with no profitable strategy are omitted.
export function getBestStrategies(seasonStartYear: number): Promise<BestStrategyVM[]> {
  const params = new URLSearchParams({ seasonStartYear: String(seasonStartYear) });
  return apiFetch<BestStrategyVM[]>(`/api/Strategy/GetBestStrategies?${params}`);
}
