import { useCallback, useRef, useState } from 'react';
import { BrowserRouter, Routes, Route, useLocation } from 'react-router-dom';
import { useEffect } from 'react';

function ScrollToTop() {
  const { pathname } = useLocation();
  useEffect(() => {
    window.scrollTo({ top: 0, behavior: 'instant' });
  }, [pathname]);
  return null;
}
import { Layout } from './components/Layout';
import { GamesPage } from './pages/GamesPage';
import { TeamsPage } from './pages/TeamsPage';
import { TeamDetailPage } from './pages/TeamDetailPage';
import { GamePage } from './pages/GamePage';
import { AboutPage } from './pages/AboutPage';
import { AdminPage } from './pages/AdminPage';
import { CrossBookStrategyPage } from './pages/CrossBookStrategyPage';
import { StrategyContext } from './contexts/StrategyContext';
import { DEFAULT_STRATEGY, type StrategyConfig } from './utils/bettingStrategies';
import { getBestStrategies, type BestStrategyVM } from './api/strategy';
import { getCurrentSeason } from './utils/season';

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
      <BrowserRouter>
        <ScrollToTop />
        <Routes>
          <Route element={<Layout />}>
            <Route path="/" element={<GamesPage />} />
            <Route path="/teams" element={<TeamsPage />} />
            <Route path="/team/:teamId" element={<TeamDetailPage />} />
            <Route path="/game/:gameId" element={<GamePage />} />
            <Route path="/strategies" element={<CrossBookStrategyPage />} />
            <Route path="/about" element={<AboutPage />} />
            <Route path="/admin" element={<AdminPage />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </StrategyContext.Provider>
  );
}
