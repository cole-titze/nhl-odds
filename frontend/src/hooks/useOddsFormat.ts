import { useState } from 'react';
import { useHydrated } from './useHydrated';

export type OddsFormat = 'pct' | 'vegas';

function getStoredFormat(): OddsFormat {
  return (localStorage.getItem('oddsFormat') as OddsFormat) || 'pct';
}

export function useOddsFormat() {
  const hydrated = useHydrated();
  // null until the user toggles; until then the stored choice applies
  const [chosen, setChosen] = useState<OddsFormat | null>(null);
  const format = chosen ?? (hydrated ? getStoredFormat() : 'pct');

  const toggle = () => {
    const next = format === 'pct' ? 'vegas' : 'pct';
    localStorage.setItem('oddsFormat', next);
    setChosen(next);
  };

  return { format, toggle };
}
