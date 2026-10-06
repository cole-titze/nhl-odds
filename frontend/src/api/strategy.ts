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

export interface SeasonResultVM {
  season: number;
  bets: number;
  roi: number;
}

export interface StrategySeasonResultsVM {
  // 'DraftKings', 'Kalshi', or 'best' (the pinned book with the largest edge)
  book: string;
  betType: BetTypeCategory;
  strategyType: StrategyType;
  threshold: number;
  seasons: SeasonResultVM[];
}

// Every strategy/threshold backtested per season over the same seasons as
// getBestStrategies, against each pinned book and the best of them. Cached by the API.
export function getStrategySeasonResults(
  seasonStartYear: number,
): Promise<StrategySeasonResultsVM[]> {
  const params = new URLSearchParams({ seasonStartYear: String(seasonStartYear) });
  return apiFetch<StrategySeasonResultsVM[]>(`/api/Strategy/GetSeasonResults?${params}`);
}
