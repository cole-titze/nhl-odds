import { useState, useEffect } from 'react';
import { useHydrated } from './useHydrated';
import { DEFAULT_TEAM, TEAM_VAR_NAMES, THEMES, teamVars, themeById, type ThemeId } from '../themes';

export type { ThemeId as Theme } from '../themes';

function getStoredTheme(): ThemeId {
  const stored = localStorage.getItem('theme');
  if (stored && themeById(stored)) return stored as ThemeId;
  return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
}

export function useTheme() {
  const hydrated = useHydrated();
  // null until the user picks; until then the stored choice applies. The server
  // and the hydration render see the defaults (theme-init.js has already styled the page).
  const [chosenTheme, setTheme] = useState<ThemeId | null>(null);
  const [chosenTeam, setTeam] = useState<string | null>(null);
  const theme = chosenTheme ?? (hydrated ? getStoredTheme() : 'light');
  const team = chosenTeam ?? (hydrated ? localStorage.getItem('themeTeam') : null) ?? DEFAULT_TEAM;

  useEffect(() => {
    // Applying the hydration render's defaults would undo theme-init.js
    if (!hydrated) return;
    const root = document.documentElement;
    const current = themeById(theme)!;
    const classes = [`theme-${theme}`];
    if (current.dark) classes.push('dark');
    if (current.scenic) classes.push('scenic');

    for (const t of THEMES) root.classList.remove(`theme-${t.id}`);
    root.classList.remove('dark', 'scenic');
    root.classList.add(...classes);

    // Team colors are inline custom properties on <html>, which beat class
    // rules, so clear them whenever another theme is active.
    const vars = theme === 'team' ? teamVars(team) : {};
    for (const name of TEAM_VAR_NAMES) root.style.removeProperty(name);
    for (const [name, value] of Object.entries(vars)) root.style.setProperty(name, value);

    const meta = document.querySelector('meta[name="theme-color"]');
    if (meta) meta.setAttribute('content', current.metaColor);

    localStorage.setItem('theme', theme);
    localStorage.setItem('themeTeam', team);
    // Snapshot for public/theme-init.js, which applies it before first paint
    localStorage.setItem(
      'themeState',
      JSON.stringify({ classes, vars, metaColor: current.metaColor }),
    );
  }, [hydrated, theme, team]);

  return { theme, setTheme, team, setTeam };
}
