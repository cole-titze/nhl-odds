import { useHydrated } from './useHydrated';

/**
 * Locale and time zone for showing game times. The server doesn't know the
 * visitor's zone, so it (and the hydration render) shows Central time; after
 * that the browser's own zone is used.
 */
export function useDisplayTimeZone(): { locales?: string; timeZone?: string } {
  return useHydrated() ? {} : { locales: 'en-US', timeZone: 'America/Chicago' };
}
