import { useEffect, useRef } from 'react';
import { Outlet, useLocation } from 'react-router';
import { Navbar } from './Navbar';
import { Assistant } from './Assistant';
import { ThemeBackdrop } from './ThemeBackdrop';
import { PHOTO_CREDITS } from '../themes';
import { StatusBarTint } from './StatusBarTint';
import { useOddsFormat } from '../hooks/useOddsFormat';
import { useActiveThemeId } from '../hooks/useRootTheme';
import { OddsFormatContext } from '../contexts/OddsFormatContext';
import { useResumeNavigation } from '../hooks/useResumeNavigation';

export function Layout() {
  const oddsFormat = useOddsFormat();
  const themeId = useActiveThemeId();
  const photo = themeId ? PHOTO_CREDITS[themeId] : undefined;
  const { pathname } = useLocation();
  useResumeNavigation();
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

    // Browsers only scroll a newly focused element if it's off screen, and Firefox doesn't
    // count its scroll-margin when deciding: a control that lands under the footer counts
    // as on screen and stays hidden (WCAG 2.4.11). Move it clear ourselves. Main itself
    // and SectionNav's sections are focused on purpose and can be taller than the screen.
    const clearFooter = (e: FocusEvent) => {
      const el = e.target;
      if (!(el instanceof HTMLElement) || !el.closest('main')) return;
      if (el.id === 'main' || el.hasAttribute('data-section')) return;
      // After the browser's own scroll (if any), so only what it left covered moves
      requestAnimationFrame(() => {
        if (document.activeElement !== el) return;
        const r = el.getBoundingClientRect();
        const footerTop = window.innerHeight - footer.offsetHeight;
        if (r.bottom > footerTop && r.top < window.innerHeight) {
          window.scrollBy({ top: r.bottom - footerTop + 8, behavior: 'instant' });
        }
      });
    };
    document.addEventListener('focusin', clearFooter);

    // With the on-screen keyboard up, iOS keeps fixed-bottom things on the layout viewport, so
    // the footer (and the chat button above it) float over the page above the keyboard. Hide
    // them while a text field outside a dialog has focus on a touch screen.
    const touch = window.matchMedia('(pointer: coarse)');
    const isTyping = (el: Element | null) =>
      el instanceof HTMLElement &&
      el.matches(
        'textarea, [contenteditable=""], [contenteditable="true"], input:not([type=checkbox], [type=radio], [type=button], [type=submit], [type=reset], [type=range], [type=color], [type=file], [type=image])',
      ) &&
      !el.closest('[role="dialog"]');
    const syncTyping = () =>
      root.classList.toggle('typing', touch.matches && isTyping(document.activeElement));
    // Focus moving between fields fires focusout before focusin; check once it lands
    const onFocusOut = () => setTimeout(syncTyping);
    document.addEventListener('focusin', syncTyping);
    document.addEventListener('focusout', onFocusOut);
    return () => {
      observer.disconnect();
      document.removeEventListener('focusin', clearFooter);
      document.removeEventListener('focusin', syncTyping);
      document.removeEventListener('focusout', onFocusOut);
      root.classList.remove('typing');
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
        <Assistant />
        {/* Fixed so it's always on screen: the games page scrolls forever and would never reach a footer */}
        <footer
          ref={footerRef}
          className="app-footer fixed inset-x-0 bottom-0 z-30 border-t border-surface-200 dark:border-white/[0.05] bg-surface-50/95 dark:bg-surface-950/95 backdrop-blur px-4 pt-1.5 pb-[max(0.375rem,env(safe-area-inset-bottom))] text-center text-[11px] leading-snug text-surface-500 dark:text-surface-400 font-mono"
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
