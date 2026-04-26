import { useCallback, useEffect, useReducer, useRef } from 'react';
import { addHours, parseISO } from 'date-fns';
import { getAnchorDate, getGameOddsInDateRange } from '../api/gameOdds';
import type { GameOddsVM } from '../types';
import { addDaysIso, formatDate } from '../utils/dates';

const HALF_CHUNK = 7;
const CHUNK_DAYS = 14;
const MAX_CONSECUTIVE_EMPTY = 3;

export type LoadStatus = 'idle' | 'loading' | 'error';

export interface PrependSnapshot {
  prevScrollHeight: number;
  prevScrollY: number;
}

interface BidiState {
  seasonStartYear: number;
  anchorDate: string | null;
  earliestLoaded: string | null;
  latestLoaded: string | null;
  gamesByDate: Map<string, GameOddsVM[]>;
  initialStatus: LoadStatus;
  olderStatus: LoadStatus;
  newerStatus: LoadStatus;
  hasMoreOlder: boolean;
  hasMoreNewer: boolean;
  consecutiveEmptyOlder: number;
  consecutiveEmptyNewer: number;
  error: string | null;
  prependPending: PrependSnapshot | null;
}

type Action =
  | { type: 'INIT'; seasonStartYear: number; explicitAnchor?: string | null }
  | { type: 'ANCHOR_RESOLVED'; anchorDate: string | null }
  | { type: 'INIT_CHUNK_SUCCESS'; gamesByDate: Map<string, GameOddsVM[]>; lower: string; upper: string }
  | { type: 'INIT_CHUNK_ERROR'; error: string }
  | { type: 'LOAD_OLDER_START' }
  | { type: 'LOAD_OLDER_SUCCESS'; games: GameOddsVM[]; newEarliest: string; isEmpty: boolean }
  | { type: 'LOAD_OLDER_ERROR'; error: string }
  | { type: 'LOAD_NEWER_START' }
  | {
      type: 'LOAD_NEWER_SUCCESS';
      games: GameOddsVM[];
      newLatest: string;
      isEmpty: boolean;
      snapshot: PrependSnapshot;
    }
  | { type: 'LOAD_NEWER_ERROR'; error: string }
  | { type: 'ACK_PREPEND' };

function emptyState(seasonStartYear: number, explicitAnchor: string | null = null): BidiState {
  return {
    seasonStartYear,
    anchorDate: explicitAnchor,
    earliestLoaded: null,
    latestLoaded: null,
    gamesByDate: new Map(),
    initialStatus: 'loading',
    olderStatus: 'idle',
    newerStatus: 'idle',
    hasMoreOlder: true,
    hasMoreNewer: true,
    consecutiveEmptyOlder: 0,
    consecutiveEmptyNewer: 0,
    error: null,
    prependPending: null,
  };
}

function reducer(state: BidiState, action: Action): BidiState {
  switch (action.type) {
    case 'INIT':
      return emptyState(action.seasonStartYear, action.explicitAnchor ?? null);

    case 'ANCHOR_RESOLVED':
      return { ...state, anchorDate: action.anchorDate };

    case 'INIT_CHUNK_SUCCESS':
      return {
        ...state,
        gamesByDate: action.gamesByDate,
        earliestLoaded: action.lower,
        latestLoaded: action.upper,
        initialStatus: 'idle',
        error: null,
      };

    case 'INIT_CHUNK_ERROR':
      return { ...state, initialStatus: 'error', error: action.error };

    case 'LOAD_OLDER_START':
      return { ...state, olderStatus: 'loading', error: null };

    case 'LOAD_OLDER_SUCCESS': {
      const merged = mergeGames(state.gamesByDate, action.games);
      const empty = action.isEmpty ? state.consecutiveEmptyOlder + 1 : 0;
      return {
        ...state,
        gamesByDate: merged,
        earliestLoaded: action.newEarliest,
        olderStatus: 'idle',
        consecutiveEmptyOlder: empty,
        hasMoreOlder: empty < MAX_CONSECUTIVE_EMPTY,
      };
    }

    case 'LOAD_OLDER_ERROR':
      return { ...state, olderStatus: 'error', error: action.error };

    case 'LOAD_NEWER_START':
      return { ...state, newerStatus: 'loading', error: null };

    case 'LOAD_NEWER_SUCCESS': {
      const merged = mergeGames(state.gamesByDate, action.games);
      const empty = action.isEmpty ? state.consecutiveEmptyNewer + 1 : 0;
      return {
        ...state,
        gamesByDate: merged,
        latestLoaded: action.newLatest,
        newerStatus: 'idle',
        consecutiveEmptyNewer: empty,
        hasMoreNewer: empty < MAX_CONSECUTIVE_EMPTY,
        prependPending: action.snapshot,
      };
    }

    case 'LOAD_NEWER_ERROR':
      return { ...state, newerStatus: 'error', error: action.error };

    case 'ACK_PREPEND':
      return { ...state, prependPending: null };

    default:
      return state;
  }
}

function mergeGames(
  existing: Map<string, GameOddsVM[]>,
  incoming: GameOddsVM[],
): Map<string, GameOddsVM[]> {
  if (incoming.length === 0) return existing;
  const next = new Map(existing);
  for (const g of incoming) {
    const key = gameDateBucket(g.gameDate);
    const prev = next.get(key);
    next.set(key, prev ? [...prev, g] : [g]);
  }
  return next;
}

// Bucket a game's UTC datetime to a yyyy-MM-dd string in Central time
// (matches the backend's GameDateUTC.AddHours(-6).Date logic).
export function gameDateBucket(isoGameDate: string): string {
  return formatDate(addHours(parseISO(isoGameDate), -6));
}

export function useBidirectionalGames(seasonStartYear: number) {
  const [state, dispatch] = useReducer(reducer, seasonStartYear, (year) => emptyState(year));

  // Epoch lets us cancel in-flight fetches when the user resets via jumpToDate / jumpToSeason.
  const epochRef = useRef(0);
  // Stable accessor so imperative callbacks read the latest state without stale closures.
  const stateRef = useRef(state);
  useEffect(() => {
    stateRef.current = state;
  });

  const loadInitialChunk = useCallback(
    async (anchor: string, season: number, myEpoch: number) => {
      const lower = addDaysIso(anchor, -HALF_CHUNK);
      const upper = addDaysIso(anchor, HALF_CHUNK);
      try {
        const games = await getGameOddsInDateRange(lower, upper, season);
        if (epochRef.current !== myEpoch) return;
        const map = new Map<string, GameOddsVM[]>();
        for (const g of games) {
          const k = gameDateBucket(g.gameDate);
          const prev = map.get(k);
          map.set(k, prev ? [...prev, g] : [g]);
        }
        dispatch({ type: 'INIT_CHUNK_SUCCESS', gamesByDate: map, lower, upper });
      } catch (err) {
        if (epochRef.current !== myEpoch) return;
        dispatch({ type: 'INIT_CHUNK_ERROR', error: (err as Error).message });
      }
    },
    [],
  );

  // Resolve anchor + initial chunk whenever season changes.
  useEffect(() => {
    epochRef.current += 1;
    const myEpoch = epochRef.current;
    dispatch({ type: 'INIT', seasonStartYear });

    (async () => {
      try {
        const { anchorDate } = await getAnchorDate(seasonStartYear);
        if (epochRef.current !== myEpoch) return;
        const anchor = anchorDate ?? formatDate(new Date());
        dispatch({ type: 'ANCHOR_RESOLVED', anchorDate: anchor });
        await loadInitialChunk(anchor, seasonStartYear, myEpoch);
      } catch (err) {
        if (epochRef.current !== myEpoch) return;
        dispatch({ type: 'INIT_CHUNK_ERROR', error: (err as Error).message });
      }
    })();
  }, [seasonStartYear, loadInitialChunk]);

  const loadOlder = useCallback(() => {
    const s = stateRef.current;
    if (
      s.initialStatus !== 'idle' ||
      s.olderStatus === 'loading' ||
      !s.hasMoreOlder ||
      s.earliestLoaded === null
    ) {
      return;
    }
    const myEpoch = epochRef.current;
    const newEarliest = addDaysIso(s.earliestLoaded, -CHUNK_DAYS);
    const fetchUpper = addDaysIso(s.earliestLoaded, -1);

    dispatch({ type: 'LOAD_OLDER_START' });
    getGameOddsInDateRange(newEarliest, fetchUpper, s.seasonStartYear)
      .then((games) => {
        if (epochRef.current !== myEpoch) return;
        dispatch({
          type: 'LOAD_OLDER_SUCCESS',
          games,
          newEarliest,
          isEmpty: games.length === 0,
        });
      })
      .catch((err: Error) => {
        if (epochRef.current !== myEpoch) return;
        dispatch({ type: 'LOAD_OLDER_ERROR', error: err.message });
      });
  }, []);

  const loadNewer = useCallback(() => {
    const s = stateRef.current;
    if (
      s.initialStatus !== 'idle' ||
      s.newerStatus === 'loading' ||
      !s.hasMoreNewer ||
      s.latestLoaded === null
    ) {
      return;
    }
    const myEpoch = epochRef.current;
    const newLatest = addDaysIso(s.latestLoaded, CHUNK_DAYS);
    const fetchLower = addDaysIso(s.latestLoaded, 1);

    dispatch({ type: 'LOAD_NEWER_START' });
    getGameOddsInDateRange(fetchLower, newLatest, s.seasonStartYear)
      .then((games) => {
        if (epochRef.current !== myEpoch) return;
        // Snapshot scroll position before the prepend dispatches — useLayoutEffect
        // in the page reads this and restores scrollTop after the DOM updates.
        const snapshot: PrependSnapshot = {
          prevScrollHeight: document.documentElement.scrollHeight,
          prevScrollY: window.scrollY,
        };
        dispatch({
          type: 'LOAD_NEWER_SUCCESS',
          games,
          newLatest,
          isEmpty: games.length === 0,
          snapshot,
        });
      })
      .catch((err: Error) => {
        if (epochRef.current !== myEpoch) return;
        dispatch({ type: 'LOAD_NEWER_ERROR', error: err.message });
      });
  }, []);

  const jumpToDate = useCallback(
    (date: Date) => {
      epochRef.current += 1;
      const myEpoch = epochRef.current;
      const anchor = formatDate(date);
      dispatch({ type: 'INIT', seasonStartYear, explicitAnchor: anchor });
      dispatch({ type: 'ANCHOR_RESOLVED', anchorDate: anchor });
      void loadInitialChunk(anchor, seasonStartYear, myEpoch);
    },
    [seasonStartYear, loadInitialChunk],
  );

  const acknowledgePrepend = useCallback(() => {
    dispatch({ type: 'ACK_PREPEND' });
  }, []);

  return {
    seasonStartYear: state.seasonStartYear,
    anchorDate: state.anchorDate,
    earliestLoaded: state.earliestLoaded,
    latestLoaded: state.latestLoaded,
    gamesByDate: state.gamesByDate,
    initialStatus: state.initialStatus,
    olderStatus: state.olderStatus,
    newerStatus: state.newerStatus,
    hasMoreOlder: state.hasMoreOlder,
    hasMoreNewer: state.hasMoreNewer,
    error: state.error,
    prependPending: state.prependPending,
    loadOlder,
    loadNewer,
    jumpToDate,
    acknowledgePrepend,
  };
}
