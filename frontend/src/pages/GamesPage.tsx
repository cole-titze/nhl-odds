import { useEffect, useLayoutEffect, useRef, useState, type KeyboardEvent } from 'react';
import { GamesFeed } from '../components/GamesFeed';
import { SeasonSelector } from '../components/SeasonSelector';
import { CardSkeleton } from '../components/Skeleton';
import { StrategyPicker } from '../components/StrategyPicker';
import { useStrategy } from '../contexts/StrategyContext';
import { LoadingStatus, PageTitle } from '../components/A11y';
import { useBidirectionalGames } from '../hooks/useBidirectionalGames';
import { toDateInputValue } from '../utils/dates';
import { getCurrentSeason } from '../utils/season';

export type OddsType = 'moneyline' | 'spread' | 'overUnder';

const FILTER_BAR_ID = 'games-filter-bar';
const FEED_ID = 'games-feed';
const TABBABLE =
  'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), [tabindex]:not([tabindex="-1"])';

// The feed starts a week before the day the page opens scrolled to, so a plain
// Tab out of the filter bar would land on an off-screen card from days ago (and
// scroll up into a load of even older games). Send it to the first item that's
// actually on screen instead, which is what comes next visually. Done on the
// Tab keypress rather than by redirecting focus when it arrives, because moving
// focus on focus is a change of context (WCAG 3.2.1).
function handleFilterBarTab(e: KeyboardEvent<HTMLDivElement>) {
  if (e.key !== 'Tab' || e.shiftKey) return;
  const inBar = [...e.currentTarget.querySelectorAll<HTMLElement>(TABBABLE)];
  if (document.activeElement !== inBar[inBar.length - 1]) return;
  const feed = document.getElementById(FEED_ID);
  if (!feed) return;
  const items = [...feed.querySelectorAll<HTMLElement>(TABBABLE)];
  // Fully below the sticky bars: an item only peeking out from under them
  // would be scrolled to, which is the jump this avoids
  const barsBottom = e.currentTarget.getBoundingClientRect().bottom;
  const firstVisible = items.find((el) => {
    const r = el.getBoundingClientRect();
    return r.height > 0 && r.top >= barsBottom - 1 && r.top < window.innerHeight;
  });
  if (!firstVisible || firstVisible === items[0]) return;
  e.preventDefault();
  // Already fully on screen; without preventScroll WebKit still scrolls it
  firstVisible.focus({ preventScroll: true });
}

// The card at the top of the view (just below the sticky bars) and how far below them
// it sits, so the view can be put back after a reflow
interface ViewAnchor {
  el: Element;
  offset: number;
  at: number;
}

function captureViewAnchor(): ViewAnchor | null {
  const feed = document.getElementById(FEED_ID);
  if (!feed) return null;
  const barsBottom = getStickyOffset();
  for (const el of feed.querySelectorAll('.grid > *')) {
    const r = el.getBoundingClientRect();
    if (r.bottom > barsBottom) return { el, offset: r.top - barsBottom, at: Date.now() };
  }
  return null;
}

// Height of the sticky navbar plus the filter bar, so date headers land below
// them. Measured because the navbar is shorter on mobile and the filter bar
// stacks into multiple rows there.
function getStickyOffset(): number {
  const navbar = document.querySelector<HTMLElement>('.app-nav');
  const filterBar = document.getElementById(FILTER_BAR_ID);
  return (navbar?.offsetHeight ?? 64) + (filterBar?.offsetHeight ?? 64) + 12;
}

function scrollToDate(date: string, behavior: ScrollBehavior): boolean {
  const el = document.getElementById(`date-${date}`);
  if (!el) return false;
  const top = el.getBoundingClientRect().top + window.scrollY - getStickyOffset();
  const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  window.scrollTo({ top: Math.max(0, top), behavior: reduceMotion ? 'instant' : behavior });
  return true;
}

type HomePosition = 'visible' | 'above' | 'below' | 'unloaded';

export function GamesPage() {
  const [oddsType, setOddsType] = useState<OddsType>('moneyline');
  const [season, setSeason] = useState(getCurrentSeason());
  const {
    anchorDate,
    homeAnchor,
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
    jumpToHome,
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
          if (e.target === top) loadOlder();
          else if (e.target === bot) loadNewer();
        }
      },
      { rootMargin: '600px 0px 600px 0px', threshold: 0 },
    );
    io.observe(top);
    io.observe(bot);
    return () => io.disconnect();
  }, [ready, loadNewer, loadOlder]);

  // Tell the browser how much of the viewport the sticky navbar + filter bar
  // (top) and the floating "Jump to today" button (bottom) cover, so tabbing
  // through the feed never leaves the focused card hidden behind them.
  useEffect(() => {
    const root = document.documentElement;
    const update = () => {
      root.style.setProperty('--sticky-top', `${getStickyOffset()}px`);
      root.style.setProperty('--sticky-bottom', '80px');
    };
    update();
    window.addEventListener('resize', update);
    return () => {
      window.removeEventListener('resize', update);
      root.style.removeProperty('--sticky-top');
      root.style.removeProperty('--sticky-bottom');
    };
  }, []);

  // Scroll to anchor date once the initial chunk is ready.
  useLayoutEffect(() => {
    if (initialStatus !== 'idle' || !anchorDate) return;
    scrollToDate(anchorDate, 'instant');
  }, [initialStatus, anchorDate]);

  // Changing the strategy or bet type adds and removes bet badges on every card (and
  // resizes the filter bar), which reflows the week of cards above the view and shoves
  // it around. Browser scroll anchoring would cover this, but Safari has none. So any
  // filter-bar interaction notes the top card, and the next renders put it back. Kept
  // briefly rather than used once, because a bet-type change re-renders twice (the
  // picker then switches to that bet type's strategy).
  const { strategy } = useStrategy();
  const viewAnchor = useRef<ViewAnchor | null>(null);
  const noteViewAnchor = () => {
    viewAnchor.current = captureViewAnchor();
  };
  useLayoutEffect(() => {
    const anchor = viewAnchor.current;
    if (!anchor || Date.now() - anchor.at > 1000 || !anchor.el.isConnected) return;
    const shift = anchor.el.getBoundingClientRect().top - getStickyOffset() - anchor.offset;
    if (Math.abs(shift) >= 1) window.scrollBy({ top: shift, behavior: 'instant' });
  }, [strategy.type, strategy.threshold, oddsType]);

  // Restore scroll position after a prepend so the user's view stays anchored
  // to the same content. Snapshot was captured at dispatch time inside the hook.
  useLayoutEffect(() => {
    if (!prependPending) return;
    const { prevScrollHeight, prevScrollY } = prependPending;
    const newHeight = document.documentElement.scrollHeight;
    window.scrollTo({ top: prevScrollY + (newHeight - prevScrollHeight) });
    acknowledgePrepend();
  }, [prependPending, acknowledgePrepend]);

  // Track where today's (home anchor) section sits relative to the viewport so
  // the "Jump to today" button only shows once the user has scrolled away.
  const isCurrentSeason = season === getCurrentSeason();
  const [homePosition, setHomePosition] = useState<HomePosition>('visible');
  // Lift the button above the site footer once it scrolls into view, so neither
  // covers the other. Set directly on the element to avoid re-rendering on scroll.
  const footerOverlapRef = useRef(0);
  const jumpButtonRef = useRef<HTMLButtonElement | null>(null);
  const setJumpButton = (el: HTMLButtonElement | null) => {
    jumpButtonRef.current = el;
    if (el) el.style.bottom = `${24 + footerOverlapRef.current}px`;
  };
  useEffect(() => {
    if (!ready) return;
    let frame = 0;
    const update = () => {
      frame = 0;
      const footer = document.querySelector('footer');
      const footerTop = footer ? footer.getBoundingClientRect().top : window.innerHeight;
      footerOverlapRef.current = Math.max(0, window.innerHeight - footerTop);
      if (jumpButtonRef.current) {
        jumpButtonRef.current.style.bottom = `${24 + footerOverlapRef.current}px`;
      }
      const el = homeAnchor ? document.getElementById(`date-${homeAnchor}`) : null;
      if (!isCurrentSeason || !el) {
        setHomePosition('unloaded');
        return;
      }
      const rect = el.getBoundingClientRect();
      if (rect.bottom < getStickyOffset()) setHomePosition('above');
      else if (rect.top > window.innerHeight) setHomePosition('below');
      else setHomePosition('visible');
    };
    const onScroll = () => {
      if (!frame) frame = requestAnimationFrame(update);
    };
    update();
    window.addEventListener('scroll', onScroll, { passive: true });
    window.addEventListener('resize', onScroll);
    return () => {
      window.removeEventListener('scroll', onScroll);
      window.removeEventListener('resize', onScroll);
      if (frame) cancelAnimationFrame(frame);
    };
  }, [ready, homeAnchor, isCurrentSeason, gamesByDate]);

  const jumpToToday = () => {
    if (!isCurrentSeason) {
      setSeason(getCurrentSeason());
      return;
    }
    if (homeAnchor && scrollToDate(homeAnchor, 'smooth')) return;
    void jumpToHome();
  };

  // Date used by the date-picker input — defaults to anchor while resolving.
  const dateInputValue = anchorDate ?? toDateInputValue(new Date());

  return (
    <div>
      <PageTitle title="Games" />
      <h1 className="sr-only">Games</h1>
      <div
        id={FILTER_BAR_ID}
        onKeyDown={handleFilterBarTab}
        onClickCapture={noteViewAnchor}
        onChangeCapture={noteViewAnchor}
        // Spans the full window like the navbar (the shell's overflow-x-clip
        // trims the scrollbar's width off 100vw); controls stay in the column
        className="app-subbar sticky top-14 md:top-16 z-40 mx-[calc(50%-50vw)] py-3 mb-6 backdrop-blur bg-white/70 dark:bg-surface-950/70 border-b border-surface-200/60 dark:border-white/[0.06]"
      >
        <div className="mx-auto max-w-6xl px-5 flex flex-col sm:flex-row items-center justify-between gap-3">
          <div className="flex items-center gap-2">
            <input
              type="date"
              value={dateInputValue}
              onChange={(e) => {
                if (!e.target.value) return;
                jumpToDate(new Date(e.target.value + 'T12:00:00'));
              }}
              aria-label="Jump to date"
              className="px-2.5 py-1.5 rounded-lg glass text-sm font-mono cursor-pointer"
            />
            <SeasonSelector value={season} onChange={setSeason} />
          </div>
          <StrategyPicker
            betType={
              oddsType === 'moneyline'
                ? 'moneyline'
                : oddsType === 'spread'
                  ? 'spread'
                  : 'overUnder'
            }
          />
          <div className="flex gap-1" role="group" aria-label="Bet type">
            {(['moneyline', 'spread', 'overUnder'] as const).map((type) => (
              <button
                key={type}
                type="button"
                aria-pressed={oddsType === type}
                onClick={() => setOddsType(type)}
                className={`px-3 py-1 text-xs font-medium rounded-full transition-colors ${
                  oddsType === type
                    ? 'bg-accent-600 text-on-accent'
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
        <div
          role="alert"
          className="glass rounded-xl text-center text-red-700 dark:text-red-400 py-8 px-4"
        >
          {error}
        </div>
      )}

      {initialLoading && (
        <div className="min-h-screen grid grid-cols-1 md:grid-cols-2 gap-4 content-start">
          <LoadingStatus label="Loading games…" />
          {Array.from({ length: 6 }).map((_, i) => (
            <CardSkeleton key={i} />
          ))}
        </div>
      )}

      {ready && (
        <div id={FEED_ID}>
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
        </div>
      )}

      {ready && homePosition !== 'visible' && (
        <button
          onClick={jumpToToday}
          ref={setJumpButton}
          className="fixed bottom-6 left-1/2 -translate-x-1/2 z-40 px-4 py-2 text-sm font-medium rounded-full bg-accent-600 text-on-accent shadow-lg hover:bg-accent-700 transition-colors"
        >
          <span aria-hidden="true">
            {homePosition === 'above' ? '↑ ' : homePosition === 'below' ? '↓ ' : ''}
          </span>
          Jump to today
        </button>
      )}
    </div>
  );
}
