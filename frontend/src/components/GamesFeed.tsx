import { type RefObject } from 'react';
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
  topSentinelRef: RefObject<HTMLDivElement | null>;
  bottomSentinelRef: RefObject<HTMLDivElement | null>;
  olderStatus: LoadStatus;
  newerStatus: LoadStatus;
  hasMoreOlder: boolean;
  hasMoreNewer: boolean;
  seasonStartYear: number;
  onLoadPrevSeason: () => void;
  onLoadNextSeason: () => void;
}

export function GamesFeed({
  gamesByDate,
  earliestLoaded,
  latestLoaded,
  oddsType,
  topSentinelRef,
  bottomSentinelRef,
  olderStatus,
  newerStatus,
  hasMoreOlder,
  hasMoreNewer,
  seasonStartYear,
  onLoadPrevSeason,
  onLoadNextSeason,
}: GamesFeedProps) {
  // Newest at top → render dates in reverse chronological order.
  const dates = enumerateDateRange(earliestLoaded, latestLoaded).reverse();
  const seasonLabel = formatSeasonLabel(seasonStartYear);
  const canLoadNext = seasonStartYear < getCurrentSeason();
  const canLoadPrev = seasonStartYear > FIRST_SEASON;

  return (
    <div>
      <div ref={topSentinelRef} aria-hidden="true" />

      {newerStatus === 'loading' && <FeedLoaderBand />}

      {!hasMoreNewer && (
        <FeedBoundary
          label={`End of ${seasonLabel}`}
          buttonLabel={canLoadNext ? `Load ${formatSeasonLabel(seasonStartYear + 1)} →` : null}
          onClick={onLoadNextSeason}
        />
      )}

      <div className="space-y-8">
        {dates.map((date) => {
          const games = gamesByDate.get(date);
          if (!games || games.length === 0) return null;
          return (
            <section key={date} id={`date-${date}`}>
              <h3 className="text-sm font-semibold uppercase tracking-wide text-surface-500 dark:text-surface-400 mb-3">
                {formatDisplayDate(parseISO(date + 'T12:00:00'))}
              </h3>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {games.map((game) => (
                  <GameCard key={game.id} game={game} oddsType={oddsType} />
                ))}
              </div>
            </section>
          );
        })}
      </div>

      {olderStatus === 'loading' && <FeedLoaderBand />}

      {!hasMoreOlder && (
        <FeedBoundary
          label={`Start of ${seasonLabel}`}
          buttonLabel={canLoadPrev ? `← Load ${formatSeasonLabel(seasonStartYear - 1)}` : null}
          onClick={onLoadPrevSeason}
        />
      )}

      <div ref={bottomSentinelRef} aria-hidden="true" />
    </div>
  );
}

function FeedLoaderBand() {
  return (
    <div className="my-6 flex items-center justify-center">
      <div className="relative overflow-hidden rounded-full glass px-4 py-2">
        <div className="absolute inset-0 skeleton-shimmer" />
        <span className="relative text-xs font-medium text-surface-500 dark:text-surface-400">
          Loading more games…
        </span>
      </div>
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
      <div className="text-xs font-semibold uppercase tracking-wide text-surface-400 dark:text-surface-500">
        {label}
      </div>
      {buttonLabel && (
        <button
          onClick={onClick}
          className="px-4 py-2 text-sm font-medium rounded-full glass text-surface-700 dark:text-surface-200 hover:text-accent-500 transition-colors"
        >
          {buttonLabel}
        </button>
      )}
    </div>
  );
}
