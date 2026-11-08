import { useEffect, useState } from 'react';

/** Answers faster than this show no loading indicator, so quick responses don't flicker. */
export const LOADING_DELAY_MS = 300;

/**
 * True once `active` has stayed true for `delay` ms; false again as soon as it turns false. For a component that
 * only exists while something loads, `useDelayedFlag(true)` hides it for its first `delay` ms.
 */
export function useDelayedFlag(active: boolean, delay = LOADING_DELAY_MS) {
  const [shown, setShown] = useState(false);
  useEffect(() => {
    if (!active) return;
    const timer = setTimeout(() => setShown(true), delay);
    return () => {
      clearTimeout(timer);
      setShown(false);
    };
  }, [active, delay]);
  return active && shown;
}
