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

      {error && <div className="text-center text-red-500 py-8">{error}</div>}

      {loading && (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {Array.from({ length: 4 }).map((_, i) => (
            <CardSkeleton key={i} />
          ))}
        </div>
      )}

      {!loading && !error && games && games.length === 0 && (
        <div className="text-center text-gray-500 dark:text-gray-400 py-12">
          No games scheduled for this date.
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
