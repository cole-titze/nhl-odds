import { useState, useEffect } from 'react';
import { DEFAULT_TEAM, TEAM_VAR_NAMES, THEMES, teamVars, themeById, type ThemeId } from '../themes';

export type { ThemeId as Theme } from '../themes';

function getInitialTheme(): ThemeId {
  const stored = localStorage.getItem('theme');
  if (stored && themeById(stored)) return stored as ThemeId;
  return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
}

export function useTheme() {
  const [theme, setTheme] = useState<ThemeId>(getInitialTheme);
  const [team, setTeam] = useState<string>(() => localStorage.getItem('themeTeam') ?? DEFAULT_TEAM);

  useEffect(() => {
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
  }, [theme, team]);

  return { theme, setTheme, team, setTeam };
}
