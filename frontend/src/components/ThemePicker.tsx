import type { ReactNode } from 'react';
import { useTheme, type Theme } from '../hooks/useTheme';
import { TEAMS, THEMES, teamLogo } from '../themes';

const svgProps = {
  xmlns: 'http://www.w3.org/2000/svg',
  className: 'h-4.5 w-4.5',
  viewBox: '0 0 20 20',
  fill: 'currentColor',
};

const strokeProps = {
  ...svgProps,
  fill: 'none',
  stroke: 'currentColor',
  strokeWidth: 1.6,
  strokeLinecap: 'round' as const,
  strokeLinejoin: 'round' as const,
};

const ICONS: Record<Theme, ReactNode> = {
  light: (
    <svg {...svgProps}>
      <path
        fillRule="evenodd"
        d="M10 2a1 1 0 011 1v1a1 1 0 11-2 0V3a1 1 0 011-1zm4 8a4 4 0 11-8 0 4 4 0 018 0zm-.464 4.95l.707.707a1 1 0 001.414-1.414l-.707-.707a1 1 0 00-1.414 1.414zm2.12-10.607a1 1 0 010 1.414l-.706.707a1 1 0 11-1.414-1.414l.707-.707a1 1 0 011.414 0zM17 11a1 1 0 100-2h-1a1 1 0 100 2h1zm-7 4a1 1 0 011 1v1a1 1 0 11-2 0v-1a1 1 0 011-1zM5.05 6.464A1 1 0 106.465 5.05l-.708-.707a1 1 0 00-1.414 1.414l.707.707zm1.414 8.486l-.707.707a1 1 0 01-1.414-1.414l.707-.707a1 1 0 011.414 1.414zM4 11a1 1 0 100-2H3a1 1 0 000 2h1z"
        clipRule="evenodd"
      />
    </svg>
  ),
  dark: (
    <svg {...svgProps}>
      <path d="M17.293 13.293A8 8 0 016.707 2.707a8.001 8.001 0 1010.586 10.586z" />
    </svg>
  ),
  stars: (
    <svg {...svgProps}>
      <path d="M10 1.5l1.9 5.1 5.1 1.9-5.1 1.9L10 15.5l-1.9-5.1L3 8.5l5.1-1.9L10 1.5zM16 13l.8 2.2 2.2.8-2.2.8L16 19l-.8-2.2-2.2-.8 2.2-.8L16 13z" />
    </svg>
  ),
  ice: (
    <svg {...strokeProps}>
      <path d="M10 2v16M3.1 6l13.8 8M3.1 14l13.8-8M10 2l-2 2m2-2l2 2m-2 14l-2-2m2 2l2-2" />
    </svg>
  ),
  aurora: (
    <svg {...strokeProps}>
      <path d="M2 13c3-6 5-6 8 0s5 6 8 0M2 8c3-4 5-4 8 0s5 4 8 0" />
    </svg>
  ),
  'contrast-light': (
    <svg {...svgProps}>
      <path
        fillRule="evenodd"
        d="M10 2a8 8 0 100 16 8 8 0 000-16zm0 1.6a6.4 6.4 0 010 12.8V3.6z"
        clipRule="evenodd"
      />
    </svg>
  ),
  'contrast-dark': (
    <svg {...svgProps}>
      <path
        fillRule="evenodd"
        d="M10 2a8 8 0 100 16 8 8 0 000-16zm0 1.6v12.8a6.4 6.4 0 010-12.8z"
        clipRule="evenodd"
      />
    </svg>
  ),
  team: (
    <svg {...strokeProps}>
      <path d="M7 2L3 5l2 3 1-.5V18h8V7.5l1 .5 2-3-4-3c-.5 1.5-1.5 2-3 2S7.5 3.5 7 2z" />
    </svg>
  ),
};

const TEAMS_BY_NAME = [...TEAMS].sort((a, b) => a.name.localeCompare(b.name));

// Shared look for the icon buttons. Each one is an icon with an invisible
// native <select> stretched over it, so tapping opens the platform picker
// (the iOS wheel on iPhone) while the navbar keeps its icon-only look.
const iconButton =
  'relative flex items-center justify-center p-2.5 rounded-lg text-surface-500 dark:text-surface-400 hover:bg-surface-100 dark:hover:bg-white/[0.06] transition-colors has-[:focus-visible]:ring-2 has-[:focus-visible]:ring-accent-500';
const overlaySelect = 'absolute inset-0 w-full h-full opacity-0 cursor-pointer appearance-none';

export function ThemePicker() {
  const { theme, setTheme, team, setTeam } = useTheme();

  return (
    <div className="flex items-center gap-1">
      {theme === 'team' && (
        <div className={iconButton} title="Choose team">
          <img src={teamLogo(team)} alt="" className="h-5 w-5 object-contain" />
          <select
            value={team}
            onChange={(e) => setTeam(e.target.value)}
            aria-label="Choose team"
            className={overlaySelect}
          >
            {TEAMS_BY_NAME.map((t) => (
              <option key={t.abbrev} value={t.abbrev}>
                {t.name}
              </option>
            ))}
          </select>
        </div>
      )}
      <div className={iconButton} title="Choose theme">
        {ICONS[theme]}
        <select
          value={theme}
          onChange={(e) => setTheme(e.target.value as Theme)}
          aria-label="Choose theme"
          className={overlaySelect}
        >
          {THEMES.map((t) => (
            <option key={t.id} value={t.id}>
              {t.label}
            </option>
          ))}
        </select>
      </div>
    </div>
  );
}
