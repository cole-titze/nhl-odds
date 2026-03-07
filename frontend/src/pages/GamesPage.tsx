import { useState } from 'react';
import { DateNav } from '../components/DateNav';
import { GameCard } from '../components/GameCard';
import { CardSkeleton } from '../components/Skeleton';
import { useFetch } from '../hooks/useFetch';
import { getGameOddsInDateRange } from '../api/gameOdds';
import { formatDate } from '../utils/dates';
import { getCurrentSeason } from '../utils/season';

export function GamesPage() {
  const [date, setDate] = useState(new Date());
  const dateStr = formatDate(date);
  const season = getCurrentSeason(date);

  const {
    data: games,
    loading,
    error,
  } = useFetch(() => getGameOddsInDateRange(dateStr, dateStr, season), [dateStr, season]);

  return (
    <div>
      <DateNav date={date} onChange={setDate} />

      {error && <div className="glass rounded-xl text-center text-red-500 py-8 px-4">{error}</div>}

      {loading && (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {Array.from({ length: 4 }).map((_, i) => (
            <CardSkeleton key={i} />
          ))}
        </div>
      )}

      {!loading && !error && games && games.length === 0 && (
        <div className="text-center py-20">
          <div className="text-surface-300 dark:text-surface-700 text-5xl mb-4">
            <svg
              className="w-12 h-12 mx-auto"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
              strokeWidth={1}
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"
              />
            </svg>
          </div>
          <p className="text-surface-400 dark:text-surface-500 font-medium">
            No games scheduled for this date.
          </p>
        </div>
      )}

      {!loading && games && games.length > 0 && (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {games.map((game) => (
            <GameCard key={game.id} game={game} />
          ))}
        </div>
      )}
    </div>
  );
}
