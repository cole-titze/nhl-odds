import { useEffect, useRef } from 'react';
import { Outlet, useLocation } from 'react-router-dom';
import { Navbar } from './Navbar';
import { ThemeBackdrop } from './ThemeBackdrop';
import { useOddsFormat } from '../hooks/useOddsFormat';
import { OddsFormatContext } from '../contexts/OddsFormatContext';

export function Layout() {
  const oddsFormat = useOddsFormat();
  const { pathname } = useLocation();
  const mainRef = useRef<HTMLElement>(null);
  const firstRender = useRef(true);

  // Client-side navigation doesn't move focus like a page load does, so keyboard
  // and screen reader users would be left on the link they just activated.
  useEffect(() => {
    if (firstRender.current) {
      firstRender.current = false;
      return;
    }
    mainRef.current?.focus({ preventScroll: true });
  }, [pathname]);

  return (
    <OddsFormatContext.Provider value={oddsFormat}>
      <div className="app-shell relative flex min-h-screen flex-col overflow-x-clip bg-surface-50 dark:bg-surface-950">
        <a
          href="#main"
          onClick={(e) => {
            e.preventDefault();
            mainRef.current?.focus();
          }}
          className="sr-only focus:not-sr-only focus:fixed focus:top-2 focus:left-2 focus:z-[100] focus:px-4 focus:py-2 focus:rounded-lg focus:bg-accent-600 focus:text-on-accent focus:font-semibold"
        >
          Skip to main content
        </a>
        <ThemeBackdrop />
        {/* Gives iOS Safari a solid, header-colored strip to tint the status bar with */}
        <div className="status-bar-tint" aria-hidden="true" />
        <Navbar />
        <main
          id="main"
          ref={mainRef}
          tabIndex={-1}
          className="relative z-10 mx-auto w-full max-w-6xl flex-1 px-5 py-8"
        >
          <Outlet />
        </main>
        <footer className="relative z-10 border-t border-surface-200 dark:border-white/[0.05] py-5 text-center text-xs text-surface-500 dark:text-surface-400 font-mono">
          This site is not affiliated with or endorsed by the NHL or any NHL team.
        </footer>
      </div>
    </OddsFormatContext.Provider>
  );
}
