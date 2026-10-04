import { useSyncExternalStore } from 'react';

// The admin key is typed into the Admin page and kept in localStorage, never in the URL,
// so it doesn't end up in browser history, proxy logs or Referer headers.
const STORAGE_KEY = 'adminKey';
const listeners = new Set<() => void>();

export function getAdminKey(): string | null {
  try {
    return localStorage.getItem(STORAGE_KEY);
  } catch {
    return null;
  }
}

function notify() {
  listeners.forEach((l) => l());
}

export function setAdminKey(key: string) {
  try {
    localStorage.setItem(STORAGE_KEY, key);
  } catch {
    /* storage unavailable — key just won't persist */
  }
  notify();
}

export function clearAdminKey() {
  try {
    localStorage.removeItem(STORAGE_KEY);
  } catch {
    /* ignore */
  }
  notify();
}

function subscribe(listener: () => void) {
  listeners.add(listener);
  window.addEventListener('storage', listener);
  return () => {
    listeners.delete(listener);
    window.removeEventListener('storage', listener);
  };
}

/** The stored admin key, or null. Re-renders when it's set or cleared (including from other tabs). */
export function useAdminKey(): string | null {
  return useSyncExternalStore(subscribe, getAdminKey, () => null);
}
