import { useEffect, useLayoutEffect, useRef, useState } from 'react';
import { GamesFeed } from '../components/GamesFeed';
import { SeasonSelector } from '../components/SeasonSelector';
import { CardSkeleton } from '../components/Skeleton';
import { useBidirectionalGames } from '../hooks/useBidirectionalGames';
import { toDateInputValue } from '../utils/dates';
import { getCurrentSeason } from '../utils/season';

export type OddsType = 'moneyline' | 'spread' | 'overUnder';

export function GamesPage() {
  const [oddsType, setOddsType] = useState<OddsType>('moneyline');
  const [season, setSeason] = useState(getCurrentSeason());
  const {
    anchorDate,
    earliestLoaded,
    latestLoaded,
    gamesByDate,
    initialStatus,
    olderStatus,
    newerStatus,
    hasMoreOlder,
    hasMoreNewer,
    error,
    prependPending,
    loadOlder,
    loadNewer,
    jumpToDate,
    acknowledgePrepend,
    seasonStartYear,
  } = useBidirectionalGames(season);

  const topSentinelRef = useRef<HTMLDivElement | null>(null);
  const bottomSentinelRef = useRef<HTMLDivElement | null>(null);

  const initialLoading = initialStatus === 'loading';
  const initialError = initialStatus === 'error';
  const ready = initialStatus === 'idle' && !!earliestLoaded && !!latestLoaded;

  // Auto-load via IntersectionObserver. rootMargin pre-fires the load 600px
  // before the user reaches the edge so the next chunk arrives without a stall.
  // Deps use `ready` (boolean) instead of earliestLoaded/latestLoaded so the
  // observer is only re-created when GamesFeed mounts/unmounts — not on every
  // chunk load. Re-creating on every chunk load caused the IO to re-fire
  // immediately for visible sentinels, cascading into repeated loads and
  // content shifts in both directions.
  useEffect(() => {
    if (!ready) return;
    const top = topSentinelRef.current;
    const bot = bottomSentinelRef.current;
    if (!top || !bot) return;

    const io = new IntersectionObserver(
      (entries) => {
        for (const e of entries) {
          if (!e.isIntersecting) continue;
          if (e.target === top) loadNewer();
          else if (e.target === bot) loadOlder();
        }
      },
      { rootMargin: '600px 0px 600px 0px', threshold: 0 },
    );
    io.observe(top);
    io.observe(bot);
    return () => io.disconnect();
  }, [ready, loadNewer, loadOlder]);

  // Restore scroll position after a prepend so the user's view stays anchored
  // to the same content. Snapshot was captured at dispatch time inside the hook.
  useLayoutEffect(() => {
    if (!prependPending) return;
    const { prevScrollHeight, prevScrollY } = prependPending;
    const newHeight = document.documentElement.scrollHeight;
    window.scrollTo({ top: prevScrollY + (newHeight - prevScrollHeight) });
    acknowledgePrepend();
  }, [prependPending, acknowledgePrepend]);

  // Date used by the date-picker input — defaults to anchor while resolving.
  const dateInputValue = anchorDate ?? toDateInputValue(new Date());

  return (
    <div>
      <div className="sticky top-16 z-10 -mx-5 px-5 py-3 mb-6 backdrop-blur bg-white/70 dark:bg-surface-950/70 border-b border-surface-200/60 dark:border-white/[0.06]">
        <div className="flex flex-col sm:flex-row items-center justify-between gap-3">
          <div className="flex items-center gap-2">
            <input
              type="date"
              value={dateInputValue}
              onChange={(e) => {
                if (!e.target.value) return;
                jumpToDate(new Date(e.target.value + 'T12:00:00'));
              }}
              className="px-2.5 py-1.5 rounded-lg glass text-sm font-mono cursor-pointer"
            />
            <SeasonSelector value={season} onChange={setSeason} />
          </div>
          <div className="flex gap-1">
            {(['moneyline', 'spread', 'overUnder'] as const).map((type) => (
              <button
                key={type}
                onClick={() => setOddsType(type)}
                className={`px-3 py-1 text-xs font-medium rounded-full transition-colors ${
                  oddsType === type
                    ? 'bg-accent-500 text-white'
                    : 'glass text-surface-500 dark:text-surface-400 hover:text-surface-700 dark:hover:text-surface-200'
                }`}
              >
                {type === 'moneyline' ? 'Moneyline' : type === 'spread' ? 'Spread' : 'Over/Under'}
              </button>
            ))}
          </div>
        </div>
      </div>

      {initialError && (
        <div className="glass rounded-xl text-center text-red-500 py-8 px-4">{error}</div>
      )}

      {initialLoading && (
        <div className="min-h-screen grid grid-cols-1 md:grid-cols-2 gap-4 content-start">
          {Array.from({ length: 6 }).map((_, i) => (
            <CardSkeleton key={i} />
          ))}
        </div>
      )}

      {ready && (
        <GamesFeed
          gamesByDate={gamesByDate}
          earliestLoaded={earliestLoaded!}
          latestLoaded={latestLoaded!}
          oddsType={oddsType}
          topSentinelRef={topSentinelRef}
          bottomSentinelRef={bottomSentinelRef}
          olderStatus={olderStatus}
          newerStatus={newerStatus}
          hasMoreOlder={hasMoreOlder}
          hasMoreNewer={hasMoreNewer}
          seasonStartYear={seasonStartYear}
          onLoadPrevSeason={() => setSeason((s) => Math.max(2009, s - 1))}
          onLoadNextSeason={() => setSeason((s) => Math.min(getCurrentSeason(), s + 1))}
        />
      )}
    </div>
  );
}
