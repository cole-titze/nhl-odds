import { useSyncExternalStore } from 'react';

// Tracks the `dark` class on <html>, which useTheme sets for every dark theme.
// Lets components react to the theme without owning theme state.
function subscribe(onChange: () => void) {
  const observer = new MutationObserver(onChange);
  observer.observe(document.documentElement, { attributes: true, attributeFilter: ['class'] });
  return () => observer.disconnect();
}

const isDark = () => document.documentElement.classList.contains('dark');

export function useIsDarkTheme(): boolean {
  return useSyncExternalStore(subscribe, isDark);
}
