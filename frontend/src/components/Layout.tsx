import { useEffect, useRef } from 'react';
import { Outlet, useLocation } from 'react-router';
import { Navbar } from './Navbar';
import { ThemeBackdrop } from './ThemeBackdrop';
import { PHOTO_CREDITS } from '../themes';
import { StatusBarTint } from './StatusBarTint';
import { useOddsFormat } from '../hooks/useOddsFormat';
import { useActiveThemeId } from '../hooks/useRootTheme';
import { OddsFormatContext } from '../contexts/OddsFormatContext';

export function Layout() {
  const oddsFormat = useOddsFormat();
  const themeId = useActiveThemeId();
  const photo = themeId ? PHOTO_CREDITS[themeId] : undefined;
  const { pathname } = useLocation();
  const mainRef = useRef<HTMLElement>(null);
  const firstRender = useRef(true);
  const footerRef = useRef<HTMLElement>(null);

  // The fixed footer covers the bottom of the viewport; publish its height so
  // keyboard focus scrolls content clear of it (see index.css).
  useEffect(() => {
    const footer = footerRef.current;
    if (!footer) return;
    const root = document.documentElement;
    const publish = () => root.style.setProperty('--footer-height', `${footer.offsetHeight}px`);
    // Set it now too: ResizeObserver only reports on the next rendering update, and focus can move first
    publish();
    const observer = new ResizeObserver(publish);
    observer.observe(footer);
    return () => {
      observer.disconnect();
      root.style.removeProperty('--footer-height');
    };
  }, []);

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
      <div className="app-shell relative flex min-h-screen flex-col bg-surface-50 dark:bg-surface-950">
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
        <StatusBarTint />
        <Navbar />
        <main
          id="main"
          ref={mainRef}
          tabIndex={-1}
          className="relative z-10 mx-auto w-full max-w-6xl flex-1 px-5 pt-8 pb-20"
        >
          <Outlet />
        </main>
        {/* Fixed so it's always on screen: the games page scrolls forever and would never reach a footer */}
        <footer
          ref={footerRef}
          className="fixed inset-x-0 bottom-0 z-30 border-t border-surface-200 dark:border-white/[0.05] bg-surface-50/95 dark:bg-surface-950/95 backdrop-blur px-4 pt-1.5 pb-[max(0.375rem,env(safe-area-inset-bottom))] text-center text-[11px] leading-snug text-surface-500 dark:text-surface-400 font-mono"
        >
          Personal, non-commercial hobby project. Not affiliated with or endorsed by the NHL or any
          NHL team.
          {photo && (
            <>
              {' '}
              Background:{' '}
              <a href={photo.url} target="_blank" rel="noreferrer" className="underline">
                {photo.credit}
              </a>{' '}
              (CC BY 4.0).
            </>
          )}
        </footer>
      </div>
    </OddsFormatContext.Provider>
  );
}

export default Layout;
