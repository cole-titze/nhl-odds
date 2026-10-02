import { useSyncExternalStore } from 'react';

// Tracks the class list on <html>, where useTheme puts `theme-<id>` (and
// `dark` for every dark theme). Lets components react to the theme without
// owning theme state.
function subscribe(onChange: () => void) {
  const observer = new MutationObserver(onChange);
  observer.observe(document.documentElement, { attributes: true, attributeFilter: ['class'] });
  return () => observer.disconnect();
}

const rootClassName = () => document.documentElement.className;

function useRootClassName(): string {
  return useSyncExternalStore(subscribe, rootClassName);
}

export function useIsDarkTheme(): boolean {
  return useRootClassName().split(' ').includes('dark');
}

/** The active theme id, read from the `theme-<id>` class. */
export function useActiveThemeId(): string | undefined {
  return useRootClassName().match(/(?:^|\s)theme-([\w-]+)/)?.[1];
}
