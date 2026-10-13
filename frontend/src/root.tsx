import { useCallback, useEffect, useRef, useState, type ReactNode } from 'react';
import { Links, Meta, Outlet, Scripts, useLocation } from 'react-router';
import './index.css';
import { useNonce } from './contexts/NonceContext';
import { StrategyContext } from './contexts/StrategyContext';
import { DEFAULT_STRATEGY, type StrategyConfig } from './utils/bettingStrategies';
import { getBestStrategies, type BestStrategyVM } from './api/strategy';
import { getCurrentSeason } from './utils/season';

export const links = () => [
  { rel: 'icon', type: 'image/svg+xml', href: '/favicon.svg' },
  { rel: 'preconnect', href: 'https://fonts.googleapis.com' },
  { rel: 'preconnect', href: 'https://fonts.gstatic.com', crossOrigin: 'anonymous' as const },
  {
    rel: 'stylesheet',
    href: 'https://fonts.googleapis.com/css2?family=Outfit:wght@400;500;600;700;800&family=JetBrains+Mono:wght@400;500;600;700&display=swap',
  },
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
