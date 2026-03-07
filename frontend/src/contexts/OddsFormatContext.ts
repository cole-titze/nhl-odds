import { createContext, useContext } from 'react';
import type { OddsFormat } from '../hooks/useOddsFormat';

export const OddsFormatContext = createContext<{
  format: OddsFormat;
  toggle: () => void;
}>({ format: 'pct', toggle: () => {} });

export const useOddsFormatContext = () => useContext(OddsFormatContext);
