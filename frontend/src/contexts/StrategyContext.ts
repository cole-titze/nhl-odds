import { createContext, useContext } from 'react';
import { DEFAULT_STRATEGY, type StrategyConfig } from '../utils/bettingStrategies';

interface StrategyContextValue {
  strategy: StrategyConfig;
  setStrategy: (s: StrategyConfig) => void;
}

export const StrategyContext = createContext<StrategyContextValue>({
  strategy: DEFAULT_STRATEGY,
  setStrategy: () => {},
});

export function useStrategy() {
  return useContext(StrategyContext);
}
