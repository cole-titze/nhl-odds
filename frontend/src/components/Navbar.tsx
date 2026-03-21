import { NavLink } from 'react-router-dom';
import { useTheme } from '../hooks/useTheme';
import { useOddsFormatContext } from '../contexts/OddsFormatContext';

const links = [
  { to: '/', label: 'Games' },
  { to: '/teams', label: 'Teams' },
  { to: '/strategies', label: 'Strategies' },
  { to: '/about', label: 'About' },
  { to: '/admin', label: 'Admin' },
];

export function Navbar() {
  const { theme, toggle } = useTheme();
  const { format, toggle: toggleOdds } = useOddsFormatContext();

  return (
    <nav className="sticky top-0 z-50 border-b border-surface-200/80 dark:border-white/[0.08] bg-white/80 dark:bg-surface-900/70 backdrop-blur-xl shadow-sm dark:shadow-black/20">
      <div className="mx-auto max-w-6xl flex items-center justify-between px-5 h-16">
        <div className="flex items-center gap-8">
          <NavLink to="/" className="flex items-center gap-2.5 group">
            <div className="w-8 h-8 rounded-lg bg-accent-500 flex items-center justify-center text-white font-bold text-sm tracking-tight shadow-lg shadow-accent-500/25">
              N
            </div>
            <span className="font-display font-bold text-lg tracking-tight">
              NHL <span className="text-accent-500">Odds</span>
            </span>
          </NavLink>
          <div className="flex items-center gap-1">
            {links.map((l) => (
              <NavLink
                key={l.to}
                to={l.to}
                className={({ isActive }) =>
                  `px-3 py-1.5 rounded-lg text-sm font-medium transition-all duration-150 ${
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
            className="px-2.5 py-1.5 rounded-lg hover:bg-surface-100 dark:hover:bg-white/[0.06] text-surface-500 dark:text-surface-400 transition-colors text-xs font-mono font-semibold"
            aria-label="Toggle odds format"
          >
            {format === 'pct' ? '%' : '+-'}
          </button>
          <button
            onClick={toggle}
            className="p-2.5 rounded-lg hover:bg-surface-100 dark:hover:bg-white/[0.06] text-surface-500 dark:text-surface-400 transition-colors"
            aria-label="Toggle dark mode"
          >
            {theme === 'dark' ? (
              <svg
                xmlns="http://www.w3.org/2000/svg"
                className="h-4.5 w-4.5"
                viewBox="0 0 20 20"
                fill="currentColor"
              >
                <path
                  fillRule="evenodd"
                  d="M10 2a1 1 0 011 1v1a1 1 0 11-2 0V3a1 1 0 011-1zm4 8a4 4 0 11-8 0 4 4 0 018 0zm-.464 4.95l.707.707a1 1 0 001.414-1.414l-.707-.707a1 1 0 00-1.414 1.414zm2.12-10.607a1 1 0 010 1.414l-.706.707a1 1 0 11-1.414-1.414l.707-.707a1 1 0 011.414 0zM17 11a1 1 0 100-2h-1a1 1 0 100 2h1zm-7 4a1 1 0 011 1v1a1 1 0 11-2 0v-1a1 1 0 011-1zM5.05 6.464A1 1 0 106.465 5.05l-.708-.707a1 1 0 00-1.414 1.414l.707.707zm1.414 8.486l-.707.707a1 1 0 01-1.414-1.414l.707-.707a1 1 0 011.414 1.414zM4 11a1 1 0 100-2H3a1 1 0 000 2h1z"
                  clipRule="evenodd"
                />
              </svg>
            ) : (
              <svg
                xmlns="http://www.w3.org/2000/svg"
                className="h-4.5 w-4.5"
                viewBox="0 0 20 20"
                fill="currentColor"
              >
                <path d="M17.293 13.293A8 8 0 016.707 2.707a8.001 8.001 0 1010.586 10.586z" />
              </svg>
            )}
          </button>
        </div>
      </div>
    </nav>
  );
}
