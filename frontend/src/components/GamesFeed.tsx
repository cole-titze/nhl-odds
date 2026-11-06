import { parseISO } from 'date-fns';
import { GameCard } from './GameCard';
import type { OddsType } from '../pages/GamesPage';
import type { GameOddsVM } from '../types';
import type { LoadStatus } from '../hooks/useBidirectionalGames';
import { enumerateDateRange, formatDisplayDate } from '../utils/dates';
import { FIRST_SEASON, formatSeasonLabel, getCurrentSeason } from '../utils/season';

interface GamesFeedProps {
  gamesByDate: Map<string, GameOddsVM[]>;
  earliestLoaded: string;
  latestLoaded: string;
  oddsType: OddsType;
  olderStatus: LoadStatus;
  newerStatus: LoadStatus;
  hasMoreOlder: boolean;
  hasMoreNewer: boolean;
  seasonStartYear: number;
  onLoadPrevSeason: () => void;
  onLoadNextSeason: () => void;
  onLoadOlder: () => void;
  onLoadNewer: () => void;
}

export function GamesFeed({
  gamesByDate,
  earliestLoaded,
  latestLoaded,
  oddsType,
  olderStatus,
  newerStatus,
  hasMoreOlder,
  hasMoreNewer,
  seasonStartYear,
  onLoadPrevSeason,
  onLoadNextSeason,
  onLoadOlder,
  onLoadNewer,
}: GamesFeedProps) {
  // Oldest at top → future games sit below the current day.
  const dates = enumerateDateRange(earliestLoaded, latestLoaded);
  const seasonLabel = formatSeasonLabel(seasonStartYear);
  const canLoadNext = seasonStartYear < getCurrentSeason();
  const canLoadPrev = seasonStartYear > FIRST_SEASON;

  return (
    <div>
      {hasMoreOlder ? (
        <ShowMoreButton label="Show earlier games" status={olderStatus} onClick={onLoadOlder} />
      ) : (
        <FeedBoundary
          label={`Start of ${seasonLabel}`}
          buttonLabel={canLoadPrev ? `← Load ${formatSeasonLabel(seasonStartYear - 1)}` : null}
          onClick={onLoadPrevSeason}
        />
      )}

      <div className="space-y-8">
        {dates.map((date) => {
          const games = gamesByDate.get(date);
          if (!games || games.length === 0) return null;
          return (
            <section key={date} id={`date-${date}`}>
              <h2 className="text-sm font-semibold uppercase tracking-wide text-surface-500 dark:text-surface-400 mb-3">
                {formatDisplayDate(parseISO(date + 'T12:00:00'))}
              </h2>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {games.map((game) => (
                  <GameCard key={game.id} game={game} oddsType={oddsType} />
                ))}
              </div>
            </section>
          );
        })}
      </div>

      {hasMoreNewer ? (
        <ShowMoreButton label="Show later games" status={newerStatus} onClick={onLoadNewer} />
      ) : (
        <FeedBoundary
          label={`End of ${seasonLabel}`}
          buttonLabel={canLoadNext ? `Load ${formatSeasonLabel(seasonStartYear + 1)} →` : null}
          onClick={onLoadNextSeason}
        />
      )}
    </div>
  );
}

// Loads two more weeks at one end of the feed. Stays mounted while loading (disabled,
// with the status announced) so focus doesn't drop. Older games go in above the view
// and the page puts the scroll back, so the games on screen stay put and the button
// moves up above the new ones.
function ShowMoreButton({
  label,
  status,
  onClick,
}: {
  label: string;
  status: LoadStatus;
  onClick: () => void;
}) {
  const loading = status === 'loading';
  return (
    <div className="my-6 flex flex-col items-center gap-2 text-center">
      <button
        type="button"
        onClick={onClick}
        aria-disabled={loading}
        className={`relative overflow-hidden px-4 py-2 text-sm font-medium rounded-full glass text-surface-700 dark:text-surface-200 transition-colors ${
          loading ? 'cursor-wait' : 'hover:text-accent-600 dark:hover:text-accent-400'
        }`}
      >
        {loading && <span className="absolute inset-0 skeleton-shimmer" aria-hidden="true" />}
        <span className="relative">
          {loading ? 'Loading…' : status === 'error' ? 'Try again' : label}
        </span>
      </button>
      <span
        role="status"
        className={status === 'error' ? 'text-xs text-red-700 dark:text-red-400' : 'sr-only'}
      >
        {loading ? 'Loading more games…' : status === 'error' ? 'Couldn’t load more games.' : ''}
      </span>
    </div>
  );
}

function FeedBoundary({
  label,
  buttonLabel,
  onClick,
}: {
  label: string;
  buttonLabel: string | null;
  onClick: () => void;
}) {
  return (
    <div className="my-8 flex flex-col items-center gap-3 text-center">
      <div className="text-xs font-semibold uppercase tracking-wide text-surface-500 dark:text-surface-400">
        {label}
      </div>
      {buttonLabel && (
        <button
          type="button"
          onClick={onClick}
          className="px-4 py-2 text-sm font-medium rounded-full glass text-surface-700 dark:text-surface-200 hover:text-accent-600 dark:hover:text-accent-400 transition-colors"
        >
          {buttonLabel}
        </button>
      )}
    </div>
  );
}
