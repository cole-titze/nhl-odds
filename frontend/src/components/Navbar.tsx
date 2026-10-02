import { useState } from 'react';
import { NavLink } from 'react-router-dom';
import { ThemePicker } from './ThemePicker';
import { useOddsFormatContext } from '../contexts/OddsFormatContext';

const links = [
  { to: '/strategies', label: 'Strategies' },
  { to: '/', label: 'Games' },
  { to: '/teams', label: 'Teams' },
  { to: '/about', label: 'About' },
  { to: '/admin', label: 'Admin' },
];

export function Navbar() {
  const { format, toggle: toggleOdds } = useOddsFormatContext();
  const [menuOpen, setMenuOpen] = useState(false);

  return (
    <nav className="app-nav sticky top-0 z-50 border-b border-surface-200/80 dark:border-white/[0.08] shadow-sm dark:shadow-black/20">
      {/* Glass lives on this child, not the nav itself; see .app-nav-glass in index.css */}
      <div className="app-nav-glass" aria-hidden="true" />
      <div className="mx-auto max-w-6xl flex items-center justify-between px-5 h-14 md:h-16">
        <div className="flex items-center gap-8">
          <NavLink to="/" className="flex items-center gap-2.5 group">
            <div className="w-8 h-8 rounded-lg bg-accent-500 flex items-center justify-center text-white font-bold text-sm tracking-tight shadow-lg shadow-accent-500/25">
              N
            </div>
            <span className="font-display font-bold text-lg tracking-tight">
              NHL <span className="text-accent-500">Odds</span>
            </span>
          </NavLink>
          <div className="hidden md:flex items-center gap-1">
            {links.map((l) => (
              <NavLink
                key={l.to}
                to={l.to}
                className={({ isActive }) =>
                  `nav-link px-3 py-1.5 rounded-lg text-sm font-medium transition-all duration-150 ${
                    isActive
                      ? 'bg-surface-100 dark:bg-white/[0.08] text-surface-900 dark:text-white'
                      : 'text-surface-500 dark:text-surface-400 hover:text-surface-900 dark:hover:text-white hover:bg-surface-100/50 dark:hover:bg-white/[0.04]'
                  }`
                }
              >
                {l.label}
              </NavLink>
            ))}
          </div>
        </div>
        <div className="flex items-center gap-1">
          <button
            onClick={toggleOdds}
            className="px-2.5 py-1.5 rounded-lg cursor-pointer hover:bg-surface-100 dark:hover:bg-white/[0.06] text-surface-500 dark:text-surface-400 transition-colors text-xs font-mono font-semibold"
            aria-label="Toggle odds format"
          >
            {format === 'pct' ? '%' : '+-'}
          </button>
          <ThemePicker />
          <button
            onClick={() => setMenuOpen(!menuOpen)}
            className="md:hidden p-2.5 rounded-lg hover:bg-surface-100 dark:hover:bg-white/[0.06] text-surface-500 dark:text-surface-400 transition-colors"
            aria-label="Toggle menu"
          >
            {menuOpen ? (
              <svg
                xmlns="http://www.w3.org/2000/svg"
                className="h-5 w-5"
                viewBox="0 0 20 20"
                fill="currentColor"
              >
                <path
                  fillRule="evenodd"
                  d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z"
                  clipRule="evenodd"
                />
              </svg>
            ) : (
              <svg
                xmlns="http://www.w3.org/2000/svg"
                className="h-5 w-5"
                viewBox="0 0 20 20"
                fill="currentColor"
              >
                <path
                  fillRule="evenodd"
                  d="M3 5a1 1 0 011-1h12a1 1 0 110 2H4a1 1 0 01-1-1zM3 10a1 1 0 011-1h12a1 1 0 110 2H4a1 1 0 01-1-1zM3 15a1 1 0 011-1h12a1 1 0 110 2H4a1 1 0 01-1-1z"
                  clipRule="evenodd"
                />
              </svg>
            )}
          </button>
        </div>
      </div>
      {menuOpen && (
        // No background of its own: the nav's glass layer spans this too, so it picks up the theme's bar
        <div className="app-nav-menu md:hidden border-t border-surface-200/80 dark:border-white/[0.08] px-5 pb-4 pt-2">
          <div className="flex flex-col gap-1">
            {links.map((l) => (
              <NavLink
                key={l.to}
                to={l.to}
                onClick={() => setMenuOpen(false)}
                className={({ isActive }) =>
                  `nav-link px-3 py-2.5 rounded-lg text-sm font-medium transition-all duration-150 ${
                    isActive
                      ? 'bg-surface-100 dark:bg-white/[0.08] text-surface-900 dark:text-white'
                      : 'text-surface-500 dark:text-surface-400 hover:text-surface-900 dark:hover:text-white hover:bg-surface-100/50 dark:hover:bg-white/[0.04]'
                  }`
                }
              >
                {l.label}
              </NavLink>
            ))}
          </div>
        </div>
      )}
    </nav>
  );
}
