import { useState } from 'react';
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { Layout } from './components/Layout';
import { GamesPage } from './pages/GamesPage';
import { TeamsPage } from './pages/TeamsPage';
import { TeamDetailPage } from './pages/TeamDetailPage';
import { GamePage } from './pages/GamePage';
import { StrategiesPage } from './pages/StrategiesPage';
import { AboutPage } from './pages/AboutPage';
import { AdminPage } from './pages/AdminPage';
import { StrategyContext } from './contexts/StrategyContext';
import { DEFAULT_STRATEGY, type StrategyConfig } from './utils/bettingStrategies';

export default function App() {
  const [strategy, setStrategy] = useState<StrategyConfig>(DEFAULT_STRATEGY);

  return (
    <StrategyContext.Provider value={{ strategy, setStrategy }}>
      <BrowserRouter>
        <Routes>
          <Route element={<Layout />}>
            <Route path="/" element={<GamesPage />} />
            <Route path="/teams" element={<TeamsPage />} />
            <Route path="/team/:teamId" element={<TeamDetailPage />} />
            <Route path="/game/:gameId" element={<GamePage />} />
            <Route path="/strategies" element={<StrategiesPage />} />
            <Route path="/about" element={<AboutPage />} />
            <Route path="/admin" element={<AdminPage />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </StrategyContext.Provider>
  );
}
