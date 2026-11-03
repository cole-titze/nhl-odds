import { useCallback, useEffect, useRef, useState, type ReactNode } from 'react';
import {
  isRouteErrorResponse,
  Links,
  Meta,
  Outlet,
  Scripts,
  useLocation,
  useRouteError,
} from 'react-router';
import './index.css';
import outfitLatin from '@fontsource-variable/outfit/files/outfit-latin-wght-normal.woff2?url';
import monoLatin from '@fontsource-variable/jetbrains-mono/files/jetbrains-mono-latin-wght-normal.woff2?url';
import { useNonce } from './contexts/NonceContext';
import { StrategyContext } from './contexts/StrategyContext';
import { DEFAULT_STRATEGY, type StrategyConfig } from './utils/bettingStrategies';
import { getBestStrategies, type BestStrategyVM } from './api/strategy';
import { getCurrentSeason } from './utils/season';
import { reportError } from './utils/errorReporting';

// The Latin fonts every page shows, fetched with the stylesheet instead of
// after it, so the text doesn't first paint in the fallback font
const fontPreload = (href: string) =>
  ({ rel: 'preload', as: 'font', type: 'font/woff2', href, crossOrigin: 'anonymous' }) as const;

export const links = () => [
  { rel: 'icon', type: 'image/svg+xml', href: '/favicon.svg' },
  fontPreload(outfitLatin),
  fontPreload(monoLatin),
];

// The document shell. public/theme-init.js adds the saved theme's classes to
// <html> before first paint, so <html> won't match the server's markup.
export function Layout({ children }: { children: ReactNode }) {
  const nonce = useNonce();
  return (
    <html lang="en" suppressHydrationWarning>
      <head>
        <meta charSet="UTF-8" />
        <meta name="viewport" content="width=device-width, initial-scale=1.0" />
        <meta name="theme-color" content="#fafafa" />
        <script src="/theme-init.js"></script>
        <Meta />
        {/* Styles need no nonce (the CSP allows 'self'), and the request's one
            can't match on hydration, so Links mustn't pick it up from context */}
        <Links nonce="" />
      </head>
      <body>
        {children}
        <Scripts nonce={nonce} />
      </body>
    </html>
  );
}

// Only on client-side navigation: on the first load this effect runs after the
// page's own (parents' effects run after children's), and would undo the games
// feed scrolling to today
function ScrollToTop() {
  const { pathname } = useLocation();
  const firstRender = useRef(true);
  useEffect(() => {
    if (firstRender.current) {
      firstRender.current = false;
      return;
    }
    window.scrollTo({ top: 0, behavior: 'instant' });
  }, [pathname]);
  return null;
}

export default function App() {
  const [strategy, setStrategyState] = useState<StrategyConfig>(DEFAULT_STRATEGY);
  const [suggested, setSuggested] = useState<BestStrategyVM[]>([]);
  const userChoseStrategy = useRef(false);

  const setStrategy = useCallback((s: StrategyConfig) => {
    userChoseStrategy.current = true;
    setStrategyState(s);
  }, []);

  // Default to the historically best moneyline strategy, unless the user has
  // already picked one before the backtest results arrive.
  useEffect(() => {
    getBestStrategies(getCurrentSeason())
      .then((best) => {
        setSuggested(best);
        const moneyline = best.find((b) => b.betType === 'moneyline');
        if (moneyline && !userChoseStrategy.current) {
          setStrategyState({ type: moneyline.strategyType, threshold: moneyline.threshold });
        }
      })
      .catch(() => {});
  }, []);

  return (
    <StrategyContext.Provider value={{ strategy, setStrategy, suggested }}>
      <ScrollToTop />
      <Outlet />
    </StrategyContext.Provider>
  );
}

// Shown instead of the page when it throws while rendering or loading, or for a
// URL no route matches. Crashes are reported (utils/errorReporting.ts); a
// missing page isn't a bug, so it isn't.
export function ErrorBoundary() {
  const error = useRouteError();
  const notFound = isRouteErrorResponse(error) && error.status === 404;

  useEffect(() => {
    if (!notFound) reportError('render', error);
  }, [error, notFound]);

  return (
    <main className="flex min-h-screen items-center justify-center bg-surface-50 px-4 text-surface-900 dark:bg-surface-950 dark:text-surface-100">
      <div className="glass max-w-md rounded-xl p-8 text-center">
        <h1 className="font-display mb-3 text-2xl font-bold tracking-tight">
          {notFound ? 'Page not found' : 'Something went wrong'}
        </h1>
        <p className="mb-6 text-surface-600 dark:text-surface-400">
          {notFound
            ? "There's no page at this address."
            : 'This page hit an error. It has been reported. Reloading may fix it.'}
        </p>
        {/* A full page load, so a crashed app starts fresh */}
        <a
          href="/"
          className="inline-block rounded-lg bg-accent-600 px-4 py-2 font-semibold text-on-accent"
        >
          Go to today&apos;s games
        </a>
      </div>
    </main>
  );
}
