import { ApiError, apiFetch } from './client';

export interface TeamResult {
  teamId: number;
  name: string;
  abbreviation: string;
  logoUri: string;
  active: boolean;
}

export interface PlayerResult {
  playerId: number;
  name: string;
  position: string | null;
  teamId: number | null;
  teamName: string | null;
  teamAbbreviation: string | null;
  isActive: boolean;
  headshot: string;
}

export interface GameSideResult {
  teamId: number;
  name: string;
  abbreviation: string;
  goals: number | null;
}

export interface GameResult {
  gameId: number;
  /** The schedule date in Eastern time, e.g. 2026-11-03 */
  gameDate: string;
  startTimeUtc: string;
  played: boolean;
  home: GameSideResult;
  away: GameSideResult;
}

export interface ArticleResult {
  title: string;
  summary: string | null;
  url: string;
  date: string | null;
  /** Body text around the match; each match sits between MATCH_START and MATCH_END */
  snippet: string | null;
}

export interface SearchResults {
  query: string;
  teams: TeamResult[];
  players: PlayerResult[];
  games: GameResult[];
  articles: ArticleResult[];
}

// Private-use characters the API puts around matched words (Search.SiteSearch.MatchStart/End)
export const MATCH_START = '';
export const MATCH_END = '';

export const MIN_QUERY_CHARS = 2;
export const MAX_QUERY_CHARS = 100;

export type SearchOutcome =
  | { kind: 'results'; results: SearchResults }
  | { kind: 'empty' }
  | { kind: 'off' }
  | { kind: 'error' };

/** Runs a search, turning the API's "too short", "search is off" and "unavailable" answers into outcomes. */
export async function search(query: string, init?: RequestInit): Promise<SearchOutcome> {
  const q = query.trim();
  if (q.length < MIN_QUERY_CHARS) return { kind: 'empty' };
  try {
    const results = await apiFetch<SearchResults>(
      `/api/Search?${new URLSearchParams({ q: q.slice(0, MAX_QUERY_CHARS) })}`,
      init,
    );
    return { kind: 'results', results };
  } catch (e) {
    if (e instanceof ApiError && e.status === 404) return { kind: 'off' };
    return { kind: 'error' };
  }
}
