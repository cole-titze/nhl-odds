import { useSyncExternalStore } from 'react';

const subscribe = () => () => {};

/**
 * False on the server and during hydration, true after. Browser-only values
 * (localStorage, time zone) must wait for it so the first client render
 * matches the server's HTML.
 */
export function useHydrated(): boolean {
  return useSyncExternalStore(
    subscribe,
    () => true,
    () => false,
  );
}
