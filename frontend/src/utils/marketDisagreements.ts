// Model vs market disagreements (#156): how far the model's win probability is
// from the market's for each game, and how the games it disagreed on most
// turned out.
//
// The market is the average vig-free probability over every book with odds
// for the game (each book's two implied probabilities scaled to sum to 1), so
// no single book's margin or quirks decide it.
import type { GameOddsVM } from '../types';
import { Winner } from '../types';
import { gameDateBucket } from './dates';

// How many of each day's games count as its biggest disagreements in the record
export const TOP_PER_DAY = 3;
// Game days shown in the recent list
const RECENT_DAYS = 14;
// Gap bands for the record by size, in probability (0.05 = 5 points)
const BANDS: { label: string; min: number; max: number }[] = [
  { label: 'Under 3 pts', min: 0, max: 0.03 },
  { label: '3–6 pts', min: 0.03, max: 0.06 },
  { label: '6–10 pts', min: 0.06, max: 0.1 },
  { label: '10+ pts', min: 0.1, max: Infinity },
];

export interface DisagreementTeam {
  id: number;
  name: string;
  logoUri: string;
  goals: number;
}

export interface DisagreementRow {
  gameId: number;
  gameDate: string;
  /** Game day in Central time (yyyy-MM-dd), like the Games feed */
  day: string;
  home: DisagreementTeam;
  away: DisagreementTeam;
  modelHome: number;
  marketHome: number;
  /** Books in the market average */
  books: string[];
  /** Size of the disagreement, in probability */
  gap: number;
  /** The team the model rates higher than the market does */
  side: 'home' | 'away';
  played: boolean;
  /** Whether the model's side won; null until played */
  sideWon: boolean | null;
}

export interface DisagreementRecord {
  games: number;
  /** Games the model's side won */
  sideWins: number;
  /** Wins the model expected for its side, and the market */
  modelExpected: number;
  marketExpected: number;
  /** Average log loss of each, on these games */
  modelLogLoss: number;
  marketLogLoss: number;
}

export interface DisagreementBand extends DisagreementRecord {
  label: string;
}

export interface DisagreementsData {
  /** The day the list leads with: today (Central) if it has games, else the next game day, else the last one */
  day: string | null;
  dayRows: DisagreementRow[];
  /** Earlier game days, newest first, with their biggest disagreements */
  recent: { day: string; rows: DisagreementRow[] }[];
  /** Each day's TOP_PER_DAY biggest disagreements, all season */
  topRecord: DisagreementRecord;
  /** Every played game, by gap size */
  bands: DisagreementBand[];
}

/** The market's vig-free home win probability, averaged over every book with odds. */
export function marketHomeProbability(game: GameOddsVM): { home: number; books: string[] } | null {
  const fair: number[] = [];
  const books: string[] = [];
  for (const b of game.bookmakerOdds ?? []) {
    if (!(b.homeOdds > 0 && b.awayOdds > 0)) continue;
    fair.push(b.homeOdds / (b.homeOdds + b.awayOdds));
    books.push(b.bookmakerName);
  }
  if (fair.length === 0) return null;
  return { home: fair.reduce((s, p) => s + p, 0) / fair.length, books };
}

export function toDisagreement(game: GameOddsVM): DisagreementRow | null {
  const { homeTeam, awayTeam } = game;
  const modelHome = homeTeam?.modelOdds;
  if (!homeTeam || !awayTeam || modelHome == null || modelHome <= 0 || modelHome >= 1) return null;
  const market = marketHomeProbability(game);
  if (!market) return null;
  const side = modelHome >= market.home ? 'home' : 'away';
  const homeWon = game.winner === Winner.HOME;
  const team = (t: NonNullable<GameOddsVM['homeTeam']>): DisagreementTeam => ({
    id: t.id,
    name: t.teamName,
    logoUri: t.logoUri,
    goals: t.goals,
  });
  return {
    gameId: game.id,
    gameDate: game.gameDate,
    day: gameDateBucket(game.gameDate),
    home: team(homeTeam),
    away: team(awayTeam),
    modelHome,
    marketHome: market.home,
    books: market.books,
    gap: Math.abs(modelHome - market.home),
    side,
    played: game.hasBeenPlayed,
    sideWon: game.hasBeenPlayed ? (side === 'home') === homeWon : null,
  };
}

/** A probability for the model's side of the row */
export const sideProbability = (row: DisagreementRow, homeProb: number) =>
  row.side === 'home' ? homeProb : 1 - homeProb;

const logLoss = (p: number, won: boolean) =>
  -Math.log(Math.min(Math.max(won ? p : 1 - p, 1e-6), 1));

export function summarize(rows: DisagreementRow[]): DisagreementRecord {
  const played = rows.filter((r) => r.played);
  const sum = (f: (r: DisagreementRow) => number) => played.reduce((s, r) => s + f(r), 0);
  const n = played.length;
  return {
    games: n,
    sideWins: played.filter((r) => r.sideWon).length,
    modelExpected: sum((r) => sideProbability(r, r.modelHome)),
    marketExpected: sum((r) => sideProbability(r, r.marketHome)),
    modelLogLoss: n ? sum((r) => logLoss(sideProbability(r, r.modelHome), !!r.sideWon)) / n : 0,
    marketLogLoss: n ? sum((r) => logLoss(sideProbability(r, r.marketHome), !!r.sideWon)) / n : 0,
  };
}

const byGap = (a: DisagreementRow, b: DisagreementRow) => b.gap - a.gap;

export function buildDisagreements(games: GameOddsVM[], now = new Date()): DisagreementsData {
  const byDay = new Map<string, DisagreementRow[]>();
  for (const g of games) {
    const row = toDisagreement(g);
    if (!row) continue;
    if (!byDay.has(row.day)) byDay.set(row.day, []);
    byDay.get(row.day)!.push(row);
  }
  const days = [...byDay.keys()].sort();
  for (const d of days) byDay.get(d)!.sort(byGap);

  const today = gameDateBucket(now.toISOString());
  const day =
    days.find((d) => d === today) ??
    days.find((d) => d > today && byDay.get(d)!.some((r) => !r.played)) ??
    days.filter((d) => d < today).at(-1) ??
    null;

  const earlier = days.filter((d) => day == null || d < day);
  const recent = earlier
    .slice(-RECENT_DAYS)
    .reverse()
    .map((d) => ({ day: d, rows: byDay.get(d)!.slice(0, TOP_PER_DAY) }));

  const allRows = days.flatMap((d) => byDay.get(d)!);
  const topRows = days.flatMap((d) => byDay.get(d)!.slice(0, TOP_PER_DAY));
  const bands = BANDS.map(({ label, min, max }) => ({
    label,
    ...summarize(allRows.filter((r) => r.gap >= min && r.gap < max)),
  }));

  return {
    day,
    dayRows: day ? byDay.get(day)! : [],
    recent,
    topRecord: summarize(topRows),
    bands,
  };
}
