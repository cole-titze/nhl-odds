import { createContext, useContext } from 'react';
import { DEFAULT_STRATEGY, type StrategyConfig } from '../utils/bettingStrategies';
import type { BestStrategyVM } from '../api/strategy';

interface StrategyContextValue {
  strategy: StrategyConfig;
  setStrategy: (s: StrategyConfig) => void;
  suggested: BestStrategyVM[];
}

export const StrategyContext = createContext<StrategyContextValue>({
  strategy: DEFAULT_STRATEGY,
  setStrategy: () => {},
  suggested: [],
});

export function useStrategy() {
  return useContext(StrategyContext);
}
