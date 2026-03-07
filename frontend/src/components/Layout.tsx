import { Outlet } from 'react-router-dom';
import { Navbar } from './Navbar';

export function Layout() {
  return (
    <div className="flex min-h-screen flex-col bg-white dark:bg-gray-900">
      <Navbar />
      <main className="mx-auto w-full max-w-5xl flex-1 px-4 py-6">
        <Outlet />
      </main>
      <footer className="py-4 text-center text-xs text-gray-400 dark:text-gray-600">
        This site is not affiliated with or endorsed by the NHL or any NHL team.
      </footer>
    </div>
  );
}
