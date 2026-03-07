import { useState } from 'react';

export type OddsFormat = 'pct' | 'vegas';

function getInitialFormat(): OddsFormat {
  return (localStorage.getItem('oddsFormat') as OddsFormat) || 'pct';
}

export function useOddsFormat() {
  const [format, setFormat] = useState<OddsFormat>(getInitialFormat);

  const toggle = () =>
    setFormat((f) => {
      const next = f === 'pct' ? 'vegas' : 'pct';
      localStorage.setItem('oddsFormat', next);
      return next;
    });

  return { format, toggle };
}
