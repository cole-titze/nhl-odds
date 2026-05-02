import { useState } from 'react';
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

export default function App() {
  const [strategy, setStrategy] = useState<StrategyConfig>(DEFAULT_STRATEGY);

  return (
    <StrategyContext.Provider value={{ strategy, setStrategy }}>
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
