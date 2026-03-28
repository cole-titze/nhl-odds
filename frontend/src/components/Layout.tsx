import { Outlet } from 'react-router-dom';
import { Navbar } from './Navbar';
import { useOddsFormat } from '../hooks/useOddsFormat';
import { OddsFormatContext } from '../contexts/OddsFormatContext';

export function Layout() {
  const oddsFormat = useOddsFormat();

  return (
    <OddsFormatContext.Provider value={oddsFormat}>
      <div className="relative flex min-h-screen flex-col overflow-x-hidden bg-surface-50 dark:bg-surface-950">
        <Navbar />
        <main className="relative z-10 mx-auto w-full max-w-6xl flex-1 px-5 py-8">
          <Outlet />
        </main>
        <footer className="relative z-10 border-t border-surface-200 dark:border-white/[0.05] py-5 text-center text-xs text-surface-400 dark:text-surface-600 font-mono">
          This site is not affiliated with or endorsed by the NHL or any NHL team.
        </footer>
      </div>
    </OddsFormatContext.Provider>
  );
}
